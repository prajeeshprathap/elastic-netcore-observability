using System;
using System.Collections.Generic;
using System.Reflection;

namespace Observability.Core
{
    public class EventHandlerConfiguration
    {
        public IDictionary<string, Type> Handlers { get; set; } = new Dictionary<string, Type>();

        public EventHandlerConfiguration RegisterConsumer<TEvent, TEventHandler>()
            where TEvent : AuditDataEvent
            where TEventHandler : IServiceEventHandler
        {
            var eventName = typeof(TEvent).GetCustomAttribute<EventAttribute>()?.Name;
            if (string.IsNullOrEmpty(eventName))
            {
                throw new InvalidOperationException($"{nameof(EventAttribute)} missing on {typeof(TEvent).Name}");
            }
            Handlers[eventName] = typeof(TEventHandler);
            return this;
        }
    }
}
