using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.ReverseProxy.Service.Proxy;
using Microsoft.ReverseProxy.Service.RuntimeModel.Transforms;
using NJsonSchema;
using NSwag;
using Weikio.ApiFramework.Abstractions;
using Weikio.ApiFramework.AspNetCore.NSwag;
using Weikio.ApiFramework.SDK;
using Endpoint = Weikio.ApiFramework.Abstractions.Endpoint;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public class OpenApiClientProxy : IEndpointMetadataExtender
    {
        private readonly ILogger<OpenApiClientProxy> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public ApiOptions Configuration { get; set; }

        public OpenApiClientProxy(ILogger<OpenApiClientProxy> logger, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        [FixedHttpConventions]
        [Route("{**catchAll}")]
        [HttpGet]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        public async Task Run(string catchAll)
        {
            var proxy = GetProxy();
            var client = GetClient();
            var context = _httpContextAccessor.HttpContext;

            var requestContext = new ApiRequestContext(Configuration, _serviceProvider, this, proxy, client, context);

            object state = null;

            if (Configuration?.BeforeRequest != null)
            {
                state = await Configuration.BeforeRequest(requestContext);
            }

            var route = context.Request.Path.ToString().Replace(catchAll, "").TrimEnd('/');
            
            var serverUrlMetaData = context.GetEndpoint()?.Metadata?.OfType<ServerUrl>().FirstOrDefault();

            var endpointUrl = string.Empty;
            if (serverUrlMetaData != null)
            {
                endpointUrl = serverUrlMetaData.Url;
            }

            if (!string.IsNullOrWhiteSpace(Configuration?.ApiUrl))
            {
                endpointUrl = Configuration.ApiUrl;
            }
            
            var additionalHeaders = new Dictionary<string, string>();
            
            if (Configuration?.ConfigureAdditionalHeaders != null)
            {
                additionalHeaders = Configuration.ConfigureAdditionalHeaders(requestContext, state);
            }

            var headerTransforms = new Dictionary<string, RequestHeaderTransform>()
            {
                { HeaderNames.Host, new RequestHeaderValueTransform(string.Empty, append: false) }
            };

            foreach (var additionalHeader in additionalHeaders)
            {
                headerTransforms.Add(additionalHeader.Key, new RequestHeaderValueTransform(additionalHeader.Value, true));
            }
            
            var proxyOptions = new RequestProxyOptions()
            {
                RequestTimeout = TimeSpan.FromSeconds(100),
                Transforms = new Transforms(
                    copyRequestHeaders: true,
                    requestTransforms: new List<RequestParametersTransform>()
                    {
                        new PathStringTransform(PathStringTransform.PathTransformMode.RemovePrefix, new PathString(route))
                    },
                    requestHeaderTransforms: headerTransforms,
                    responseHeaderTransforms: new Dictionary<string, ResponseHeaderTransform>(),
                    responseTrailerTransforms: new Dictionary<string, ResponseHeaderTransform>())
            };

            await proxy.ProxyAsync(context, endpointUrl, client, proxyOptions);
        }
        private static string _proxyLock = "lock";
        private static IHttpProxy _proxy = null;

        private static string _clientLock = "lock";
        private static HttpMessageInvoker _client = null;

        private IHttpProxy GetProxy()
        {
            if (_proxy != null)
            {
                return _proxy;
            }

            lock (_proxyLock)
            {
                if (_proxy == null)
                {
                    var sc = new ServiceCollection();
                    sc.AddHttpProxy();
                    var loggerType = _logger.GetType().GetGenericTypeDefinition();

                    sc.AddTransient(typeof(ILogger<>), loggerType);
                    sc.AddSingleton(typeof(ILoggerFactory), _loggerFactory.GetType());

                    var sp = sc.BuildServiceProvider();
                    _proxy = sp.GetServices<IHttpProxy>().First();
                }

                return _proxy;
            }
        }

        private HttpMessageInvoker GetClient()
        {
            if (_client != null)
            {
                return _client;
            }

            lock (_clientLock)
            {
                if (_client == null)
                {
                    _client = new HttpMessageInvoker(new SocketsHttpHandler()
                    {
                        UseProxy = false, AllowAutoRedirect = false, AutomaticDecompression = DecompressionMethods.None, UseCookies = false
                    });
                }

                return _client;
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [FixedHttpConventions]
        public async Task<List<object>> GetMetadata(Endpoint endpoint)
        {
            var config = (ApiOptions) endpoint.Configuration;
            var openApiDocument = await OpenApiDocument.FromUrlAsync(config.SpecificationUrl);

            var additionalOperationPaths = new Dictionary<string, OpenApiPathItem>();
            var additionalSchemas = new List<KeyValuePair<string, JsonSchema>>();

            foreach (var path in openApiDocument.Paths)
            {
                additionalOperationPaths.Add(endpoint.Route + path.Key, path.Value);
            }

            foreach (var openApiSchema in openApiDocument.Components.Schemas)
            {
                additionalSchemas.Add(openApiSchema);
            }

            var defaultUrl = openApiDocument.Servers?.FirstOrDefault()?.Url ?? string.Empty;

            var openApiDocumentExtensions = new OpenApiDocumentExtensions(additionalOperationPaths, additionalSchemas);

            return new List<object> { openApiDocumentExtensions, new ServerUrl(defaultUrl) };
        }
    }
}
