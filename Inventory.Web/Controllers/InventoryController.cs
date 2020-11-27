using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Inventory.Web.Events;
using Inventory.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Observability.Core;

namespace Inventory.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IMessageProducer messageProducer;
        private readonly IInventoryRepository repository;
        private readonly ITracer tracer;

        public InventoryController(
            IMessageProducer messageProducer
            , IInventoryRepository repository
            , ITracer tracer)
        {
            this.messageProducer = messageProducer;
            this.repository = repository;
            this.tracer = tracer;
        }

        [HttpGet]
        [Route("{product}")]
        public async Task<IActionResult> GetAsync(string product)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"GetInventory", ApiConstants.TypeRequest)
                        .WithLabel("Inventory: product", product)
                : tracer.CurrentTransaction;

            if (string.IsNullOrEmpty(product))
            {
                return BadRequest("product should not be empty");
            }

            var quantity = await repository.GetAvailableQuantityAsync(product, CancellationToken.None);

            if (isnew) transaction.End();

            return new OkObjectResult(quantity);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> NewAsync([FromBody] Contracts.Inventory inventory)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"NewInventory", "post")
                        .WithLabel("inventory: product", inventory.Product)
                        .WithLabel("inventory: quantity", inventory.Quantity.ToString())
                : tracer.CurrentTransaction;

            if (inventory == default(Contracts.Inventory))
            {
                return BadRequest("Body empty or null");
            }

            inventory.Id = Guid.NewGuid();

            await repository.NewAsync(inventory, CancellationToken.None);

            messageProducer.SendMessage(new InventoryAddedEvent
            {
                Product = inventory.Product,
                Quantity = inventory.Quantity
            });

            if (isnew) transaction.End();
            return Ok();
        }
    }
}
