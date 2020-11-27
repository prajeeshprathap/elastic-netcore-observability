using Newtonsoft.Json;
using System;

namespace Orders.Web.Contracts
{
    public class Inventory
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
}
