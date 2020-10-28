using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NJsonSchema;
using NSwag;
using Weikio.ApiFramework.Abstractions;
using Weikio.ApiFramework.AspNetCore.NSwag;

namespace Weikio.ApiFramework.Plugins.OpenApi.Proxy
{
    public class NSwagMetadataExtender
    {
        private readonly IEndpointRouteTemplateProvider _endpointRouteTemplateProvider;

        public NSwagMetadataExtender(IEndpointRouteTemplateProvider endpointRouteTemplateProvider)
        {
            _endpointRouteTemplateProvider = endpointRouteTemplateProvider;
        }

        public virtual async Task<List<object>> GetMetadata(Endpoint endpoint, ApiOptions config)
        {
            var openApiDocument = await OpenApiDocument.FromUrlAsync(config.SpecificationUrl);

            var additionalOperationPaths = new Dictionary<string, OpenApiPathItem>();
            var additionalSchemas = new List<KeyValuePair<string, JsonSchema>>();

            var routeTemplate = _endpointRouteTemplateProvider.GetRouteTemplate(endpoint);

            var transformedPaths = new Dictionary<string, OpenApiPathItem>();

            foreach (var path in openApiDocument.Paths)
            {
                var transformedPath = config.TransformPath(path.Key, path.Value, config);
                transformedPaths.Add(transformedPath.Item1, transformedPath.Item2);
            }

            foreach (var path in transformedPaths)
            {
                var isIncluded = config.IncludePath(path.Key, path.Value, config);

                if (!isIncluded)
                {
                    continue;
                }

                var isExcluded = config.ExcludePath(path.Key, path.Value, config);

                if (isExcluded)
                {
                    continue;
                }

                var includedOperations = new Dictionary<string, OpenApiOperation>();

                foreach (var operationId in path.Value.Keys)
                {
                    var operation = path.Value[operationId];

                    var isOperationIncluded = config.IncludeOperation(operationId, operation, config);

                    if (!isOperationIncluded)
                    {
                        continue;
                    }

                    var isOperationExcluded = config.ExcludeOperation(operationId, operation, config);

                    if (isOperationExcluded)
                    {
                        continue;
                    }

                    var transformedOperation = config.TransformOperation(operationId, operation, config);

                    includedOperations.Add(transformedOperation.Item1, transformedOperation.Item2);
                }

                path.Value.Clear();
                path.Value.AddRange(includedOperations);

                additionalOperationPaths.Add(routeTemplate + path.Key, path.Value);
            }

            // TODO: These could be filtered based on the operation/path filterings
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
