using Elastic.Apm.Api;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Observability.Core;
using Observability.MongoDb;
using Orders.Web.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Orders.Web.Repositories
{
    public class CustomerRepository : MongoDbRepository, ICustomerRepository
    {
        private readonly ITracer tracer;

        IMongoCollection<Customer> Customers;

        public CustomerRepository(IOptions<MongoDbConfiguration> mongoDbOptions, ITracer tracer) : base(mongoDbOptions)
        {
            this.tracer = tracer; 
            BsonDefaults.GuidRepresentation = GuidRepresentation.CSharpLegacy;
            Customers = MongoDatabase.GetCollection<Customer>("Customer");
        }

        public async Task<Customer> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"GetById", "customerdb")
                        .WithLabel("Id", id.ToString())
                : tracer.CurrentTransaction;

            var span = transaction.StartSpan($"Get customer", "mongodb", subType: ApiConstants.TypeExternal);
            span.SetLabel("Customer id", id.ToString());

            var customer = await Task.FromResult(Customers.Find(c => c.Id == id, new FindOptions { AllowPartialResults = false }).FirstOrDefault(cancellationToken)).ConfigureAwait(false);
            span?.End();
            if (isnew) transaction.End();
            return customer;
        }

        public async Task NewAsync(Customer customer, CancellationToken cancellationToken)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"New", "customerdb")
                        .WithLabel("Id", customer.Id.ToString())
                : tracer.CurrentTransaction;

            var span = transaction.StartSpan($"New customer", "mongodb", subType: ApiConstants.TypeExternal);
            span.SetLabel("Customer id", customer.Id.ToString());
            span.SetLabel("Customer name", customer.Name.ToString());

            await Customers.InsertOneAsync(customer, new InsertOneOptions { BypassDocumentValidation = false }, cancellationToken).ConfigureAwait(false);

            span?.End();
            if (isnew) transaction.End();
        }
    }
}
