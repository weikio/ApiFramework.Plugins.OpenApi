using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public abstract class OpenApiClientBase
    {
        private readonly ApiOptions _apiOptions;

        public OpenApiClientBase(ApiOptions apiOptions)
        {
            _apiOptions = apiOptions;
        }

        protected Task<HttpClient> CreateHttpClientAsync(CancellationToken cancellationToken)
        {
            var httpClient = HttpClientFactory.CreateClient(_apiOptions);
            return Task.FromResult(httpClient);
        }
    }
}
