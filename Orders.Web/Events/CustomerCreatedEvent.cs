using Observability.Core;
using System;

namespace Orders.Web.Events
{
    [Event("CustomerCreated")]
    public class CustomerCreatedEvent : AuditDataEvent
    {
        public Guid CustomerId { get; set; }
    }
}
