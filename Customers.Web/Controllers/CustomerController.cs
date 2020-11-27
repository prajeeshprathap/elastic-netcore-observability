using Customers.Web.Contracts;
using Customers.Web.Events;
using Customers.Web.Repositories;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Observability.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly IMessageProducer messageProducer;
        private readonly ICustomerRepository repository;
        private readonly ITracer tracer;

        public CustomerController(
            IMessageProducer messageProducer
            , ICustomerRepository repository
            , ITracer tracer)
        {
            this.messageProducer = messageProducer;
            this.repository = repository;
            this.tracer = tracer;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetAsync(Guid id)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"GetCustomer", ApiConstants.TypeRequest)
                        .WithLabel("Customer: Id", id.ToString())
                : tracer.CurrentTransaction;

            if (id == Guid.Empty)
            {
                return BadRequest("id should not be empty");
            }

            var customer = await repository.GetByIdAsync(id, CancellationToken.None);

            if (isnew) transaction.End();

            return customer == default ? new NotFoundResult() : (IActionResult)new OkObjectResult(customer);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> NewAsync([FromBody] Customer customer)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"NewCustomer", "post")
                        .WithLabel("customer: id", customer.Id.ToString())
                        .WithLabel("customer: name", customer.Name)
                : tracer.CurrentTransaction;

            if (customer == default)
            {
                return BadRequest("Body empty or null");
            }

            customer.Id = Guid.NewGuid();

            await repository.NewAsync(customer, CancellationToken.None);

            messageProducer.SendMessage(new CustomerCreatedEvent
            {
                CustomerId = customer.Id
            });

            if (isnew) transaction.End();
            return Ok();
        }
    }
}
