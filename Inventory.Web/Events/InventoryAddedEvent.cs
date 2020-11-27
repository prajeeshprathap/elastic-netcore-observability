using Observability.Core;
using System;

namespace Inventory.Web.Events
{
    [Event("InventoryAdded")]
    public class InventoryAddedEvent : AuditDataEvent
    {
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
}
