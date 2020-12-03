using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ReverseProxy.Service.RuntimeModel.Transforms;
using NSwag;
using Weikio.ApiFramework.Abstractions;
using Weikio.ApiFramework.Plugins.OpenApi.Proxy;

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

        public ApiMode Mode { get; set; } = ApiMode.Proxy;
        public TagTransformModeEnum TagTransformMode { get; set; } = TagTransformModeEnum.UseEndpointNameOrRoute;

        public AuthenticationOptions Authentication { get; set; }

        public HttpClientOptions HttpClient { get; set; } = new HttpClientOptions();

        public Func<ApiRequestContext, Task<object>> BeforeRequest = null;
        public Func<ApiRequestContext, object, Dictionary<string, string>> ConfigureAdditionalHeaders = null;

        public Func<ApiRequestContext, object, List<RequestParametersTransform>, List<RequestParametersTransform>> ConfigureRequestParameterTransforms =
            (context, state, defaultTransforms) => defaultTransforms;

        public Func<string, OpenApiPathItem, ApiOptions, bool> IncludePath { get; set; } = (path, item, options) => true;
        public Func<string, OpenApiPathItem, ApiOptions, bool> ExcludePath { get; set; } = (path, item, options) => false;
        public Func<string, OpenApiOperation, ApiOptions, bool> IncludeOperation { get; set; } = (operationId, item, options) => true;
        public Func<string, OpenApiOperation, ApiOptions, bool> ExcludeOperation { get; set; } = (operationId, item, options) => false;
        public Func<string, OpenApiPathItem, ApiOptions, (string, OpenApiPathItem)> TransformPath { get; set; } = (path, item, options) => (path, item);
        public Func<string, OpenApiOperation, ApiOptions, (string, OpenApiOperation)> TransformOperation { get; set; } = (path, item, options) => (path, item);
        public Func<Endpoint, OpenApiOperation, ApiOptions, List<string>, List<string>> TransformTags { get; set; } =
            (endpoint, operation, options, originalTags) =>
            {
                if (options.TagTransformMode == TagTransformModeEnum.UseOriginal)
                {
                    return originalTags;
                }
                
                if (options.TagTransformMode == TagTransformModeEnum.UseEndpointNameOrRoute)
                {
                    return new List<string>() { string.IsNullOrWhiteSpace(endpoint.Name) ? endpoint.Route : endpoint.Name };
                }

                if (originalTags == null)
                {
                    originalTags = new List<string>();
                }
                
                originalTags.Add(string.IsNullOrWhiteSpace(endpoint.Name) ? endpoint.Route : endpoint.Name);

                return originalTags;
            };
    }
}
