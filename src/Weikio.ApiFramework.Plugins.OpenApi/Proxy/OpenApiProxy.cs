using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.ReverseProxy.Service.Proxy;
using Microsoft.ReverseProxy.Service.RuntimeModel.Transforms;

namespace Weikio.ApiFramework.Plugins.OpenApi.Proxy
{
    public class OpenApiProxy
    {
        private readonly ILogger<OpenApiProxy> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public ApiOptions Configuration { get; set; }

        public OpenApiProxy(ILogger<OpenApiProxy> logger, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider, ApiOptions configuration)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;

            Configuration = configuration;
        }

        public async Task RunRequest(string catchAll, ApiOptions overrideOptions = null)
        {
            var proxy = GetProxy();
            var client = GetClient();
            var context = _httpContextAccessor.HttpContext;

            var config = Configuration;

            if (overrideOptions != null)
            {
                config = overrideOptions;
            }

            var requestContext = new ApiRequestContext(config, _serviceProvider, this, proxy, client, context);

            object state = null;

            if (config?.BeforeRequest != null)
            {
                state = await config.BeforeRequest(requestContext);
            }

            var route = context.Request.Path.ToString().Replace(catchAll, "").TrimEnd('/');

            var serverUrlMetaData = context.GetEndpoint()?.Metadata?.OfType<ServerUrl>().FirstOrDefault();

            var endpointUrl = string.Empty;

            if (serverUrlMetaData != null)
            {
                endpointUrl = serverUrlMetaData.Url;
            }

            if (!string.IsNullOrWhiteSpace(config?.ApiUrl))
            {
                endpointUrl = config.ApiUrl;
            }

            var additionalHeaders = new Dictionary<string, string>();

            if (config?.ConfigureAdditionalHeaders != null)
            {
                additionalHeaders = config.ConfigureAdditionalHeaders(requestContext, state);
            }

            var headerTransforms = new Dictionary<string, RequestHeaderTransform>()
            {
                { HeaderNames.Host, new RequestHeaderValueTransform(string.Empty, append: false) },
            };

            if (config?.RemoveCookieHeader == true)
            {
                headerTransforms.Add(HeaderNames.Cookie, new RequestHeaderValueTransform(string.Empty, false));
            }

            foreach (var additionalHeader in additionalHeaders)
            {
                headerTransforms.Add(additionalHeader.Key, new RequestHeaderValueTransform(additionalHeader.Value, true));
            }

            var requestParameterTransforms = new List<RequestParametersTransform>()
            {
                new PathStringTransform(PathStringTransform.PathTransformMode.RemovePrefix, new PathString(route)),
            };

            if (!string.IsNullOrWhiteSpace(config?.TrimPath))
            {
                // If we have trimmed the path, add it back when calling the backend
                requestParameterTransforms.Add(new PathStringTransform(PathStringTransform.PathTransformMode.Prefix, config.TrimPath));
            }
            
            if (config?.ConfigureRequestParameterTransforms != null)
            {
                requestParameterTransforms = config.ConfigureRequestParameterTransforms(requestContext, state, requestParameterTransforms);
            }

            var proxyOptions = new RequestProxyOptions()
            {
                RequestTimeout = Configuration.HttpClient?.Timeout ?? TimeSpan.FromSeconds(100),
                Transforms = new Transforms(
                    copyRequestHeaders: true,
                    requestTransforms: requestParameterTransforms,
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

        protected virtual IHttpProxy GetProxy()
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

        protected virtual HttpMessageInvoker GetClient()
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
    }
}
