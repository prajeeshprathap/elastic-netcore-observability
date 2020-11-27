using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace Observability.Core
{
    public class FaultHandlingHttpClient : IFaultHandlingHttpClient
    {
        private IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FaultHandlingHttpClient> _logger;
        public FaultHandlingHttpClient(IHttpClientFactory httpClientFactory, ILogger<FaultHandlingHttpClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<T> GetAsync<T>(string apiUrl) 
        {
            T result = default;

            using var client = _httpClientFactory.CreateClient("FaultHandlingHttpClient");
            var response = await client.GetAsync(new Uri(apiUrl)).ConfigureAwait(true);
            response.EnsureSuccessStatusCode();
            await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
            {
                if (x.IsFaulted)
                {
                    _logger.LogError("{exception}", x.Exception);
                    throw x.Exception;
                }

                result = JsonConvert.DeserializeObject<T>(x.Result);
            });

            return result;
        }

        public async Task PostRequestAsync<T>(string apiUrl, T postObject) where T : class
        {
            _logger.LogDebug("Invoking {apiUrl}", apiUrl);

            using var client = _httpClientFactory.CreateClient("FaultHandlingHttpClient");
            var response = await client.PostAsync(apiUrl, postObject, new JsonMediaTypeFormatter()).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }
}
