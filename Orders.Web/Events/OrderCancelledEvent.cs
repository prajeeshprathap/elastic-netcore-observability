using Observability.Core;
using System;

namespace Orders.Web.Events
{
    [Event("OrderCancelled")]
    public class OrderCancelledEvent : AuditDataEvent
    {
        public Guid OrderId { get; set; }
        public string Product { get; set; }
    }
}
