using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Observability.Core
{
    public interface IFaultHandlingHttpClient
    {
        Task<T> GetAsync<T>(string apiUrl);
        Task PostRequestAsync<T>(string apiUrl, T postObject) where T : class;
    }
}
