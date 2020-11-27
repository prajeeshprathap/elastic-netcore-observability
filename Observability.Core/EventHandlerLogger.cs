using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Observability.Core
{
    public class EventHandlerLogger<THandler, TEvent>
        where THandler : IServiceEventHandler
        where TEvent : AuditDataEvent
    {
        private readonly ILogger<THandler> logger;

        public EventHandlerLogger(ILogger<THandler> logger)
        {
            this.logger = logger;
        }

        public void Information(TEvent @event)
        {
            var logData = new
            {
                Type = "EventHandler",
                Event = @event.GetType().Name,
                Data = @event
            };

            logger.LogInformation("Type: {type}, {event} handled : {data}", "EventHandler", @event.GetType().Name, JObject.FromObject(logData).ToString());
        }

        public void Information(string message, params object[] args)
        {
            logger.LogInformation(message, args);
        }
    }
}
