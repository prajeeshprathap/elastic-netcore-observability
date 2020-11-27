using Elastic.Apm.Api;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Observability.Core;
using Observability.MongoDb;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Web.Repositories
{
    public class InventoryRepository : MongoDbRepository, IInventoryRepository
    {
        private readonly ITracer tracer;

        IMongoCollection<Contracts.Inventory> Inventory;

        public InventoryRepository(IOptions<MongoDbConfiguration> mongoDbOptions, ITracer tracer) : base(mongoDbOptions)
        {
            BsonDefaults.GuidRepresentation = GuidRepresentation.CSharpLegacy;
            Inventory = MongoDatabase.GetCollection<Contracts.Inventory>("Inventory");
            this.tracer = tracer;
        }

        public async Task<int> GetAvailableQuantityAsync(string product, CancellationToken cancellationToken)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"GetInventory", "inventorydb")
                        .WithLabel("Product", product)
                : tracer.CurrentTransaction;

            var span = transaction.StartSpan($"Get inventory", "mongodb", subType: ApiConstants.TypeExternal);
            span.SetLabel("Product", product);
            var inventory = await Task.FromResult(Inventory.Find(i => i.Product == product, new FindOptions { AllowPartialResults = false }).FirstOrDefault(cancellationToken)).ConfigureAwait(false);
            var availableQuantity = inventory == null ? 0 : inventory.Quantity;
            span.SetLabel("Quantity", availableQuantity.ToString());
            span?.End();

            if (isnew) transaction.End();
            return availableQuantity;
        }

        public async Task NewAsync(Contracts.Inventory inventory, CancellationToken cancellationToken)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"New", "inventorydb")
                        .WithLabel("product", inventory.Product)
                : tracer.CurrentTransaction;

            var span = transaction.StartSpan($"New inventory", "mongodb", subType: ApiConstants.TypeExternal);
            span.SetLabel("Product", inventory.Product);
            span.SetLabel("Quantity", inventory.Quantity.ToString());

            await Inventory.InsertOneAsync(inventory, new InsertOneOptions { BypassDocumentValidation = false }, cancellationToken).ConfigureAwait(false);

            span?.End();
            if (isnew) transaction.End();
        }

        public async Task UpdateAsync(string product, int orderedQuantity, CancellationToken cancellationToken)
        {
            var isnew = tracer.CurrentTransaction == null;
            var transaction = isnew
                ? tracer.StartTransaction($"UpdateInventory", "inventorydb")
                        .WithLabel("Product", product)
                : tracer.CurrentTransaction;

            var span = transaction.StartSpan($"Get inventory", "mongodb", subType: ApiConstants.TypeExternal);
            span.SetLabel("Product", product);

            var currentInventory = await Task.FromResult(Inventory.Find(i => i.Product == product, new FindOptions { AllowPartialResults = false }).FirstOrDefault(cancellationToken)).ConfigureAwait(false);
            if (currentInventory == default(Contracts.Inventory))
            {
                throw new ArgumentException($"Failed to find inventory for product {product}");
            }
            currentInventory.Quantity -= orderedQuantity;
            await Inventory.ReplaceOneAsync(i => i.Product == product, currentInventory, new ReplaceOptions { IsUpsert = false }, cancellationToken)
                .ConfigureAwait(false);

            span.SetLabel("New Quantity", currentInventory.Quantity.ToString());
            span?.End();

            if (isnew) transaction.End();
        }
    }
}
