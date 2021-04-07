using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.ReverseProxy.Service.RuntimeModel.Transforms;
using NJsonSchema;
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

        /// <summary>
        /// Gets or sets a prefix which is added to operation ids and schemas
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets a string which is trimmed from the beginning or from the end of the path
        /// </summary>
        public string TrimPath { get; set; }

        public ApiMode Mode { get; set; } = ApiMode.Proxy;
        public TagTransformModeEnum TagTransformMode { get; set; } = TagTransformModeEnum.UseEndpointNameOrRoute;
        public PrefixMode PrefixMode { get; set; } = PrefixMode.AutoPrefixWithRouteOrCustomPrefix;
        public AuthenticationOptions Authentication { get; set; }
        public Dictionary<string, string> AdditionalHeaders { get; set; } = new Dictionary<string, string>();
        public HttpClientOptions HttpClient { get; set; } = new HttpClientOptions();

        public Func<ApiRequestContext, Task<object>> BeforeRequest = null;

        public Func<ApiRequestContext, object, Dictionary<string, string>> ConfigureAdditionalHeaders = (context, o) => context.Options.AdditionalHeaders;

        public Func<ApiRequestContext, object, List<RequestParametersTransform>, List<RequestParametersTransform>> ConfigureRequestParameterTransforms =
            (context, state, defaultTransforms) => defaultTransforms;

        public bool RemoveCookieHeader { get; set; } = false;

        public Func<string, OpenApiPathItem, ApiOptions, bool> IncludePath { get; set; } = (path, item, options) => true;
        public Func<string, OpenApiPathItem, ApiOptions, bool> ExcludePath { get; set; } = (path, item, options) => false;

        public Func<string, OpenApiOperation, ApiOptions, bool> IncludeOperation { get; set; } = (operationId, item, options) =>
        {
            if (options.IncludeHttpMethods?.Any() != true && options.ExcludeHttpMethods?.Any() != true)
            {
                return true;
            }

            if (options.ExcludeHttpMethods?.Any() == true)
            {
                if (options.ExcludeHttpMethods.Contains(operationId, StringComparer.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }

            if (options.IncludeHttpMethods?.Any() == true)
            {
                if (options.IncludeHttpMethods.Contains(operationId, StringComparer.InvariantCultureIgnoreCase))
                {
                    return true;
                }

                return false;
            }

            return true;
        };

        public Func<string, OpenApiOperation, ApiOptions, bool> ExcludeOperation { get; set; } = (operationId, item, options) => false;

        public string[] IncludeHttpMethods { get; set; }
        public string[] ExcludeHttpMethods { get; set; }

        public Func<string, OpenApiPathItem, string, string, ApiOptions, (string, OpenApiPathItem)> TransformPath { get; set; } =
            (path, item, routeTemplate, apiAddressBase, options) =>
            {
                var trimmedPath = path;

                if (!string.IsNullOrWhiteSpace(options.TrimPath))
                {
                    trimmedPath = Regex.Replace(path, $"^{options.TrimPath}", "", RegexOptions.Compiled);
                }

                return (routeTemplate + trimmedPath, item);
            };

        public Func<KeyValuePair<string, JsonSchema>, string, ApiOptions, KeyValuePair<string, JsonSchema>> TransformSchema { get; set; } =
            (schemaItem, routeTemplate, options) =>
            {
                if (string.IsNullOrWhiteSpace(options.Prefix) && options.PrefixMode == PrefixMode.AutoPrefixWithRouteOrCustomPrefix)
                {
                    var schemaWithRoute = new KeyValuePair<string, JsonSchema>(routeTemplate.Replace("/", "_").Trim('_') + schemaItem.Key, schemaItem.Value);

                    return schemaWithRoute;
                }

                var result = new KeyValuePair<string, JsonSchema>((options.Prefix + schemaItem.Key).Replace("/", "_").Trim('_'), schemaItem.Value);

                return result;
            };

        public Func<string, OpenApiOperation, string, string, ApiOptions, (string, OpenApiOperation)> TransformOperation { get; set; } =
            (method, item, routeTemplate, apiAddressBase, options) =>
            {
                if (string.IsNullOrWhiteSpace(options.Prefix) && options.PrefixMode == PrefixMode.AutoPrefixWithRouteOrCustomPrefix)
                {
                    item.OperationId = routeTemplate.Replace("/", "_").Trim('_') + item.OperationId;
                }
                else
                {
                    item.OperationId = (options.Prefix ?? "") + item.OperationId;
                }

                return (method, item);
            };

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
