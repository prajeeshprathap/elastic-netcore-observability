using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Observability.Core;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Observability.RabbitMq
{
    public class RabbitMqMessageConsumer : IMessageConsumer
    {
        private readonly ILogger<RabbitMqMessageConsumer> _logger;
        private readonly EventHandlerConfiguration _eventHandlerConfiguration;
        private readonly IServiceProvider _serviceProvider;
        private IConnection _connection;
        private IModel _channel;
        private string _queueName;
        private readonly IOptions<RabbitMqConfiguration> _rabbitMqConfig;

        public RabbitMqMessageConsumer(IServiceProvider serviceProvider,
            IOptions<RabbitMqConfiguration> rabbitMqConfig,
            ILogger<RabbitMqMessageConsumer> logger,
            EventHandlerConfiguration eventHandlerConfiguration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _eventHandlerConfiguration = eventHandlerConfiguration;
            _rabbitMqConfig = rabbitMqConfig;

            InitializeRabbitMqListener();
        }

        private void InitializeRabbitMqListener()
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqConfig.Value.Hostname,
                UserName = _rabbitMqConfig.Value.UserName,
                Password = _rabbitMqConfig.Value.Password
            };

            _connection = factory.CreateConnection();
            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: _rabbitMqConfig.Value.Exchange, type: ExchangeType.Fanout);
            _queueName = _channel.QueueDeclare().QueueName;
        }

        public void RegisterOnMessageHandlerAndReceiveMessages()
        {
            _channel.QueueBind(_queueName, _rabbitMqConfig.Value.Exchange, "netcoremicroservices-key");
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                ProcessMessageAsync(content, CancellationToken.None).GetAwaiter()
                                                                    .GetResult();
            };

            _channel.BasicConsume(queue: _queueName,
                               autoAck: true,
                               consumer: consumer);

            consumer.Shutdown += OnConsumerShutdown;
            consumer.Registered += OnConsumerRegistered;
            consumer.Unregistered += OnConsumerUnregistered;
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;
        }

        private async Task ProcessMessageAsync(string content, CancellationToken cancellationToken)
        {
            var isValid = true;
            var message = JsonConvert.DeserializeObject<RabbitMqMessage>(content,
                new JsonSerializerSettings
                {
                    Error = delegate (object sender, ErrorEventArgs args)
                    {
                        isValid = false;
                        _logger.LogWarning("{deserializerwarning} {data}", args.ErrorContext.Error.Message, content);
                        args.ErrorContext.Handled = true;
                    }
                });

            if (!isValid) { return; }
            var handlerType = GetHandlerType(message);
            if (handlerType == null) { return; }

            using var scope = _serviceProvider.CreateScope();

            var handler = GetHandler(scope, handlerType);
            _logger.LogDebug("Received event {data}", Regex.Unescape(JsonConvert.SerializeObject(message)));

            await handler.HandleAsync(JObject.Parse(message.EventData), cancellationToken).ConfigureAwait(false);
        }


        internal Type GetHandlerType(RabbitMqMessage message)
        {
            return _eventHandlerConfiguration.Handlers.TryGetValue(message.Name, out var handlerType) ? handlerType : null;
        }

        internal static IServiceEventHandler GetHandler(IServiceScope scope, Type handlerType)
        {
            var handler = scope.ServiceProvider.GetService(handlerType);

            if (handler == null)
            {
                var nullRefEx = new NullReferenceException($"No handler found for type <{handlerType}>");
                throw nullRefEx;
            }

            if (handler is IServiceEventHandler eventHandler)
            {
                return eventHandler;
            }

            var castEx = new InvalidCastException($"Handler <{handlerType}> not of type <{typeof(IServiceEventHandler)}>");
            throw castEx;
        }

        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e)
        {
            _logger.LogInformation($"RabbitMqConsumer cancelled");
        }

        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e)
        {
            _logger.LogInformation($"RabbitMqConsumer unregistered");
        }

        private void OnConsumerRegistered(object sender, ConsumerEventArgs e)
        {
            _logger.LogInformation($"RabbitMqConsumer registered");
        }

        private void OnConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogInformation("RabbitMq consumer shut down {reason}", e.ReplyText);
        }

        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogInformation("RabbitMq connection shut down {reason}", e.ReplyText);
        }

        public void CloseSubscriptionClientAsync()
        {
            _channel.Close();
            _connection.Close();
        }
    }
}
