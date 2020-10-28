using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.ReverseProxy.Service.Proxy;
using NSwag;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public class ApiOptions
    {
        /// <summary>
        /// Swagger/Open Api specification URL
        /// </summary>
        public string SpecificationUrl { get; set; }
        
        /// <summary>
        /// If set, overrides the default API url. Default is automatically loaded from the specification.
        /// </summary>
        public string ApiUrl { get; set; }

        public ApiMode Mode { get; set; } = ApiMode.Client;
        
        public AuthenticationOptions Authentication { get; set; }

        public HttpClientOptions HttpClient { get; set; } = new HttpClientOptions();

        public Func<ApiRequestContext, Task<object>> BeforeRequest = null;
        public Func<ApiRequestContext, object, Dictionary<string, string>> ConfigureAdditionalHeaders = null;

        public Func<string, OpenApiPathItem, ApiOptions, bool> IncludePath { get; set; } = (path, item, options) => true;
        public Func<string, OpenApiPathItem, ApiOptions, bool> ExcludePath { get; set; } = (path, item, options) => false;
        public Func<string, OpenApiOperation, ApiOptions, bool> IncludeOperation { get; set; } = (operationId, item, options) => true;
        public Func<string, OpenApiOperation, ApiOptions, bool> ExcludeOperation { get; set; } = (operationId, item, options) => false;
        public Func<string, OpenApiPathItem, ApiOptions, (string, OpenApiPathItem)> TransformPath { get; set; } = (path, item, options) => (path, item);
        public Func<string, OpenApiOperation, ApiOptions, (string, OpenApiOperation)> TransformOperation { get; set; } = (path, item, options) => (path, item);
    }

    public enum ApiMode
    {
        Client = 0,
        Proxy = 1
    }
    
    public class ApiRequestContext
    {
        public ApiOptions Options { get; }
        public IServiceProvider Sp { get; }
        public OpenApiClientProxy ClientProxy { get; }
        public IHttpProxy Proxy { get; }
        public HttpMessageInvoker HttpMessageInvoker { get; }
        public HttpContext HttpContext { get; }

        public ApiRequestContext(ApiOptions options, IServiceProvider sp, OpenApiClientProxy clientProxy, IHttpProxy proxy, HttpMessageInvoker httpMessageInvoker, HttpContext httpContext)
        {
            Options = options;
            Sp = sp;
            ClientProxy = clientProxy;
            Proxy = proxy;
            HttpMessageInvoker = httpMessageInvoker;
            HttpContext = httpContext;
        }
    }

    public class AuthenticationOptions
    {
        public string Basic { get; set; }

        public string Bearer { get; set; }
    }

    public class HttpClientOptions
    {
        public TimeSpan? SlidingExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Request timeout. Use standard TimeSpan string format.
        /// </summary>
        public string Timeout { get; set; }
    }
}
