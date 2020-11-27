using Elastic.Apm;
using Elastic.Apm.Api;
using Newtonsoft.Json.Linq;
using Observability.Core;
using Orders.Web.Contracts;
using Orders.Web.Events;
using Orders.Web.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace Orders.Web.EventHandlers
{
    public class CustomerCreatedEventHandler : IServiceEventHandler
    {
        private readonly EventHandlerLogger<CustomerCreatedEventHandler, CustomerCreatedEvent> logger;
        private readonly IFaultHandlingHttpClient httpClient;
        private readonly ICustomerRepository repository;
        private readonly ExternalApis apis;
        private readonly ITracer tracer;

        public CustomerCreatedEventHandler(
            EventHandlerLogger<CustomerCreatedEventHandler, CustomerCreatedEvent> logger,
            IFaultHandlingHttpClient httpClient,
            ICustomerRepository repository,
            ExternalApis apis, ITracer tracer)
        {

            this.logger = logger;
            this.httpClient = httpClient;
            this.repository = repository;
            this.apis = apis;
            this.tracer = tracer;
        }
        public async Task HandleAsync(JObject jObject, CancellationToken cancellationToken)
        {
            var @event = jObject.ToObject<CustomerCreatedEvent>();

            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction("CustomerCreatedEventHandler", "event-subscriber")
                        .WithLabel("data", jObject.ToString())
                        .WithLabel("event name", "CustomerCreatedEvent")
                : tracer.CurrentTransaction;

            logger.Information(@event);
            var apiSpan = transaction.StartSpan($"{apis.Customer}/{@event.CustomerId}", ApiConstants.ActionQuery, subType: ApiConstants.SubtypeHttp);
            var customer = await httpClient.GetAsync<Customer>($"{apis.Customer}/{@event.CustomerId}").ConfigureAwait(false);

            apiSpan.SetLabel("Response", JObject.FromObject(customer).ToString());
            apiSpan?.End();
            logger.Information("customer service {api} {RequestMethod} {data}", apis.Customer, "GET", JObject.FromObject(customer).ToString());

            await repository.NewAsync(customer, cancellationToken);

            if (isnew) transaction.End();
        }
    }
}
