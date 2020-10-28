using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Weikio.ApiFramework.Abstractions;
using Weikio.ApiFramework.SDK;
using Endpoint = Weikio.ApiFramework.Abstractions.Endpoint;

namespace Weikio.ApiFramework.Plugins.OpenApi.Proxy
{
    public class OpenApiClientProxy : IEndpointMetadataExtender
    {
        private readonly ILogger<OpenApiClientProxy> _logger;
        private readonly ILogger<OpenApiProxy> _proxyLogger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEndpointRouteTemplateProvider _endpointRouteTemplateProvider;

        public ApiOptions Configuration { get; set; }

        public OpenApiClientProxy(ILogger<OpenApiClientProxy> logger, ILogger<OpenApiProxy> proxyLogger, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider, IEndpointRouteTemplateProvider endpointRouteTemplateProvider)
        {
            _logger = logger;
            _proxyLogger = proxyLogger;
            _loggerFactory = loggerFactory;
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
            _endpointRouteTemplateProvider = endpointRouteTemplateProvider;
        }

        [FixedHttpConventions]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("{**catchAll}")]
        [HttpGet]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        public async Task Run(string catchAll)
        {
            var proxy = new OpenApiProxy(_proxyLogger, _loggerFactory, _httpContextAccessor, _serviceProvider, Configuration);

            await proxy.RunRequest(catchAll);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [NonAction]
        [FixedHttpConventions]
        public async Task<List<object>> GetMetadata(Endpoint endpoint)
        {
            var nswagExtender = new NSwagMetadataExtender(_endpointRouteTemplateProvider);

            return await nswagExtender.GetMetadata(endpoint, (ApiOptions) endpoint.Configuration);
        }
    }
}
