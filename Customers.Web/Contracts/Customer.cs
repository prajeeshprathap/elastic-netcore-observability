using Newtonsoft.Json;
using System;
using System.Text;

namespace Customers.Web.Contracts
{
    public class Customer
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
