using Observability.Core;
using System;

namespace Orders.Web.Events
{
    [Event("OrderProcessed")]
    public class OrderProcessedEvent : AuditDataEvent
    {
        public Guid OrderId { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
}
