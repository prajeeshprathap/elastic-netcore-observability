using Elastic.Apm;
using Elastic.Apm.Api;
using Inventory.Web.Events;
using Inventory.Web.Repositories;
using Newtonsoft.Json.Linq;
using Observability.Core;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Web.EventHandlers
{
    public class OrderProcessedEventHandler : IServiceEventHandler
    {
        private readonly EventHandlerLogger<OrderProcessedEventHandler, OrderProcessedEvent> logger;
        private readonly IMessageProducer messageProducer;
        private readonly IInventoryRepository repository;
        private readonly ITracer tracer;

        public OrderProcessedEventHandler(
            EventHandlerLogger<OrderProcessedEventHandler
            , OrderProcessedEvent> logger
            , IMessageProducer messageProducer
            , IInventoryRepository repository
            , ITracer tracer)
        {

            this.logger = logger;
            this.messageProducer = messageProducer;
            this.repository = repository;
            this.tracer = tracer;
        }

        public async Task HandleAsync(JObject jObject, CancellationToken cancellationToken)
        {
            var @event = jObject.ToObject<OrderProcessedEvent>();

            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction("OrderProcessedEventHandler", "event-subscriber")
                        .WithLabel("data", jObject.ToString())
                        .WithLabel("event name", "OrderProcessedEvent")

                : tracer.CurrentTransaction;

            logger.Information(@event);
            var currentQuantity = await repository.GetAvailableQuantityAsync(@event.Product, cancellationToken);
            await repository.UpdateAsync(@event.Product, @event.Quantity, cancellationToken);

            messageProducer.SendMessage(new InventoryUpdatedEvent
            {
                Product = @event.Product,
                Quantity = currentQuantity - @event.Quantity
            });

            if (isnew) transaction.End();
        }
    }
}
