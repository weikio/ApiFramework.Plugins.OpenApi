using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public abstract class OpenApiClientBase
    {
        public ApiOptions Configuration { get; set; }

        protected Task<HttpClient> CreateHttpClientAsync(CancellationToken cancellationToken)
        {
            var httpClient = HttpClientFactory.CreateClient(Configuration);
            return Task.FromResult(httpClient);
        }
    }
}
