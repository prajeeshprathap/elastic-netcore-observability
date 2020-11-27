using Observability.Core;
using System;

namespace Customers.Web.Events
{
    [Event("CustomerCreated")]
    public class CustomerCreatedEvent : AuditDataEvent
    {
        public Guid CustomerId { get; set; }
    }
}
