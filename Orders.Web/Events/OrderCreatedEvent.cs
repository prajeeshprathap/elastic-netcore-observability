using Observability.Core;
using System;

namespace Orders.Web.Events
{
    [Event("OrderCreated")]
    public class OrderCreatedEvent : AuditDataEvent
    {
        public Guid OrderId { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
}
