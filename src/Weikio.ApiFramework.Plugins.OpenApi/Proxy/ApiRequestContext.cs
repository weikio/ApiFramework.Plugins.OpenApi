using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.ReverseProxy.Service.Proxy;

namespace Weikio.ApiFramework.Plugins.OpenApi.Proxy
{
    public class ApiRequestContext
    {
        public ApiOptions Options { get; }
        public IServiceProvider Sp { get; }
        public OpenApiProxy ClientProxy { get; }
        public IHttpProxy Proxy { get; }
        public HttpMessageInvoker HttpMessageInvoker { get; }
        public HttpContext HttpContext { get; }

        public ApiRequestContext(ApiOptions options, IServiceProvider sp, OpenApiProxy clientProxy, IHttpProxy proxy, HttpMessageInvoker httpMessageInvoker, HttpContext httpContext)
        {
            Options = options;
            Sp = sp;
            ClientProxy = clientProxy;
            Proxy = proxy;
            HttpMessageInvoker = httpMessageInvoker;
            HttpContext = httpContext;
        }
    }
}
