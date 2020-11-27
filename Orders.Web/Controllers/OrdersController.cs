using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Observability.Core;
using Orders.Web.Contracts;
using Orders.Web.Events;
using Orders.Web.Repositories;

namespace Orders.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ILogger<OrdersController> logger;
        private readonly IMessageProducer messageProducer;
        private readonly ExternalApis apis;
        private readonly IFaultHandlingHttpClient httpClient;
        private readonly IOrderRepository repository;
        private readonly ICustomerRepository customerRepository;
        private readonly ITracer tracer;

        public OrdersController(ILogger<OrdersController> logger
            , IMessageProducer messageProducer
            , ExternalApis apis
            , IFaultHandlingHttpClient httpClient
            , IOrderRepository repository
            , ICustomerRepository customerRepository
            , ITracer tracer)
        {
            this.logger = logger;
            this.messageProducer = messageProducer;
            this.apis = apis;
            this.httpClient = httpClient;
            this.repository = repository;
            this.customerRepository = customerRepository;
            this.tracer = tracer;
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> NewAsync([FromBody] Order order)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"NewOrder", "post")
                        .WithLabel("Order: OrderId", order.Id.ToString())
                        .WithLabel("Order: Product", order.Product)
                        .WithLabel("Order: Quantity", order.Quantity.ToString())
                        .WithLabel("Order: Customer", order.CustomerId.ToString())
                : tracer.CurrentTransaction;


            if (order == default(Order))
            {
                return BadRequest("Body empty or null");
            }

            order.Id = Guid.NewGuid();

            var customer = await customerRepository.GetByIdAsync(order.CustomerId, CancellationToken.None);
            if(customer == default)
            {
                return NotFound("Customer");
            }

            await repository.NewAsync(order, CancellationToken.None);

            messageProducer.SendMessage(new OrderCreatedEvent
            {
                OrderId = order.Id,
                Product = order.Product,
                Quantity = order.Quantity
            });

            var span = transaction.StartSpan($"{apis.Inventory}/{order.Product}", ApiConstants.ActionQuery, subType: ApiConstants.SubtypeHttp);
            var availableQuantity = await httpClient.GetAsync<int>($"{apis.Inventory}/{order.Product}").ConfigureAwait(false);

            span.SetLabel("Response", availableQuantity.ToString());
            span?.End();
            logger.LogInformation("inventory service {api} {RequestMethod} {data}", apis.Inventory, "GET", availableQuantity.ToString());

            if (availableQuantity >= order.Quantity)
            {
                await repository.ProcessOrderAsync(order.Id, CancellationToken.None);

                messageProducer.SendMessage(new OrderProcessedEvent
                {
                    OrderId = order.Id,
                    Product = order.Product,
                    Quantity = order.Quantity
                });
            }
            else
            {
                await repository.CancelOrderAsync(order.Id, CancellationToken.None);

                messageProducer.SendMessage(new OrderCancelledEvent
                {
                    OrderId = order.Id,
                    Product = order.Product
                });
            }
            if (isnew) transaction.End();
            return Ok();
        }
    }
}
