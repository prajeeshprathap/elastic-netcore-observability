using Observability.Core;

namespace Inventory.Web.Events
{
    [Event("InventoryUpdated")]
    public class InventoryUpdatedEvent : AuditDataEvent
    {
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
}
