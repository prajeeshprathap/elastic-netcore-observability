using Newtonsoft.Json;
using System;

namespace Orders.Web.Contracts
{
    public class Order
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public OrderStatus Status { get; set; }
    }

    public enum OrderStatus
    {
        Created,
        Processed,
        Cancelled
    }
}
