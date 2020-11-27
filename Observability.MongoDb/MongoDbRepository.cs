using Elastic.Apm.Api;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Observability.MongoDb
{
    public class MongoDbRepository
    {
        protected readonly IMongoDatabase MongoDatabase;

        protected MongoDbRepository(IOptions<MongoDbConfiguration> mongoDbOptions)
        {
            var client = new MongoClient(mongoDbOptions.Value.ServerConnection);
            MongoDatabase = client.GetDatabase(mongoDbOptions.Value.Database);
        }
    }
}
