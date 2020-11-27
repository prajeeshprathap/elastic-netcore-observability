using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Observability.Core
{
    public interface IServiceEventHandler
    {
        Task HandleAsync(JObject jObject, CancellationToken cancellationToken);
    }
}
