using Observability.Core;
using System;

namespace Inventory.Web.Events
{
    [Event("OrderProcessed")]
    public class OrderProcessedEvent : AuditDataEvent
    {
        public Guid OrderId { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
}
