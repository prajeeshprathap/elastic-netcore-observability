using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Observability.Core;
using RabbitMQ.Client;
using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Observability.RabbitMq
{
    public class RabbitMqMessageProducer : IMessageProducer
    {
        private readonly ILogger<RabbitMqMessageProducer> _logger;
        private readonly ITracer tracer;
        private readonly IOptions<RabbitMqConfiguration> _rabbitMqConfig;

        public RabbitMqMessageProducer(IOptions<RabbitMqConfiguration> rabbitMqConfig, ILogger<RabbitMqMessageProducer> logger, ITracer tracer)
        {
            _logger = logger;
            this.tracer = tracer;
            _rabbitMqConfig = rabbitMqConfig;
        }

        public void SendMessage(AuditDataEvent @event)
        {
            (string name, string data) message = GetMessage(@event);
            var shouldDispose = tracer.CurrentTransaction == null;
            var transaction = shouldDispose ? tracer.StartTransaction($"{message.name}", "event-publisher") : tracer.CurrentTransaction;
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _rabbitMqConfig.Value.Hostname,
                    UserName = _rabbitMqConfig.Value.UserName,
                    Password = _rabbitMqConfig.Value.Password
                };

                var span = transaction.StartSpan($"Send message to exchange {_rabbitMqConfig.Value.Exchange}", "rabbitmq", subType: ApiConstants.TypeExternal);
                span.SetLabel("Message name", message.name);
                span.SetLabel("Message content", message.data);

                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: _rabbitMqConfig.Value.Exchange, type: ExchangeType.Fanout, durable: false, autoDelete: false, arguments: null);

                    var json = message.data;
                    var body = Encoding.UTF8.GetBytes(json);
                    channel.BasicPublish(exchange: _rabbitMqConfig.Value.Exchange, routingKey: "netcoremicroservices-key", basicProperties: null, body: body);
                }

                span?.End();
                var logData = new
                {
                    Type = "EventProducer",
                    Event = message.name,
                    Data = @event
                };
                _logger.LogInformation("Type: {type}, event send {event} on topic {topic} : {data}", "EventProducer", message.name, _rabbitMqConfig.Value.Exchange, JObject.FromObject(logData).ToString());
            }
            catch (Exception e)
            {
                transaction.CaptureException(e);
                _logger.LogError(e.Message);
            }
            if (shouldDispose) transaction.End();
        }

        private (string name, string data) GetMessage(AuditDataEvent @event)
        {
            var attribute = @event.GetType().GetCustomAttribute<EventAttribute>();

            if (attribute == null)
            {
                throw new ArgumentException($"{nameof(EventAttribute)} missing on {nameof(@event)}");
            }

            if (string.IsNullOrEmpty(attribute.Name))
            {
                throw new ArgumentNullException(
                    $"{nameof(EventAttribute)}.Name missing on {nameof(@event)}");
            }

            var serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var message = new RabbitMqMessage
            {
                Name = attribute.Name,
                EventData = JsonConvert.SerializeObject(@event, serializerSettings)
            };

            return (attribute.Name, JsonConvert.SerializeObject(message, serializerSettings));
        }
    }
}
