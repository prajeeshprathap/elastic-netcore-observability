using Newtonsoft.Json;
using System;

namespace Inventory.Web.Contracts
{
    public class Inventory
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
}
