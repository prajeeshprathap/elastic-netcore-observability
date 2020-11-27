using Elastic.Apm.Api;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Observability.Core;
using Observability.MongoDb;
using Orders.Web.Contracts;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Orders.Web.Repositories
{
    public class OrderRepository : MongoDbRepository, IOrderRepository
    {
        private readonly ITracer tracer;

        IMongoCollection<Order> Orders;
        public OrderRepository(IOptions<MongoDbConfiguration> mongoDbOptions, ITracer tracer) : base(mongoDbOptions)
        {
            BsonDefaults.GuidRepresentation = GuidRepresentation.CSharpLegacy;
            Orders = MongoDatabase.GetCollection<Order>("Order");
            this.tracer = tracer;
        }

        public async Task<Order> GetyByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"GetById", "orderdb")
                        .WithLabel("OrderId", id.ToString())
                : tracer.CurrentTransaction;

            var span = transaction.StartSpan($"Get order", "mongodb", subType: ApiConstants.TypeExternal);
            span.SetLabel("OrderId", id.ToString());

            var order = await Task.FromResult(Orders.Find(c => c.Id == id, new FindOptions { AllowPartialResults = false }).FirstOrDefault(cancellationToken)).ConfigureAwait(false);
            span?.End();
            if (isnew) transaction.End();
            return order;
        }

        public async Task NewAsync(Order order, CancellationToken cancellationToken)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"New", "orderdb")
                        .WithLabel("OrderId", order.Id.ToString())
                : tracer.CurrentTransaction;

            var span = transaction.StartSpan($"New order", "mongodb", subType: ApiConstants.TypeExternal);
            span.SetLabel("OrderId", order.Id.ToString());
            span.SetLabel("CustomerId", order.CustomerId.ToString());
            span.SetLabel("Product", order.Product);
            span.SetLabel("Quantity", order.Quantity.ToString());
            await Orders.InsertOneAsync(order, new InsertOneOptions { BypassDocumentValidation = false }, cancellationToken);
            span?.End();

            if (isnew) transaction.End();
        }

        public async Task ProcessOrderAsync(Guid id, CancellationToken cancellationToken)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"Process order", "orderdb")
                        .WithLabel("OrderId", id.ToString())
                : tracer.CurrentTransaction;


            var order = await GetyByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (order == default(Order))
            {
                throw new ArgumentException($"Failed to find a order with ID {id}");
            }
            order.Status = OrderStatus.Processed;

            var span = transaction.StartSpan($"Update order", "mongodb", subType: ApiConstants.TypeExternal);
            span.SetLabel("OrderId", order.Id.ToString());
            span.SetLabel("Status", "Processed");

            await Orders.ReplaceOneAsync(c => c.Id == id, order, new ReplaceOptions { IsUpsert = false }, cancellationToken).ConfigureAwait(false);
            span?.End();

            if (isnew) transaction.End();
        }

        public async Task CancelOrderAsync(Guid id, CancellationToken cancellationToken)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"Cancel order", "orderdb")
                        .WithLabel("OrderId", id.ToString())
                : tracer.CurrentTransaction;

            var order = await GetyByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (order == default(Order))
            {
                throw new ArgumentException($"Failed to find a order with ID {id}");
            }
            order.Status = OrderStatus.Cancelled;

            var span = transaction.StartSpan($"Update order", "mongodb", subType: ApiConstants.TypeExternal);
            span.SetLabel("OrderId", order.Id.ToString());
            span.SetLabel("Status", "Cancelled");
            await Orders.ReplaceOneAsync(c => c.Id == id, order, new ReplaceOptions { IsUpsert = false }, cancellationToken).ConfigureAwait(false);
            span?.End();

            if (isnew) transaction.End();
        }
    }
}
