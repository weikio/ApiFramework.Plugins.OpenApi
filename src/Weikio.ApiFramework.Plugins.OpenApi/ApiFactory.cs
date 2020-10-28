using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LamarCompiler;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using Weikio.ApiFramework.Plugins.OpenApi.Proxy;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public static class ApiFactory
    {
        public static async Task<IEnumerable<Type>> Create(string endpointRoute, ApiOptions configuration)
        {
            if (configuration.Mode == ApiMode.Proxy)
            {
                return new List<Type>() { typeof(OpenApiClientProxy) };
            }
            
            var openApiDocument = await OpenApiDocument.FromUrlAsync(configuration.SpecificationUrl);

            var clientGeneratorSettings = new CSharpClientGeneratorSettings
            {
                ClassName = "{controller}Api",
                InjectHttpClient = false,
                UseHttpClientCreationMethod = true,
                DisposeHttpClient = false,
                ClientBaseClass = typeof(OpenApiClientBase).FullName,
                GenerateOptionalParameters = true,
                ParameterArrayType = "System.Collections.Generic.List",
                CSharpGeneratorSettings = { Namespace = GetNamespace(endpointRoute) }
            };

            var defaultTemplateFactory = clientGeneratorSettings.CSharpGeneratorSettings.TemplateFactory;
            clientGeneratorSettings.CSharpGeneratorSettings.TemplateFactory = new TemplateFactory(defaultTemplateFactory);

            var clientGenerator = new CSharpClientGenerator(openApiDocument, clientGeneratorSettings);
            var clientCode = clientGenerator.GenerateFile();

            var assemblyGenerator = new AssemblyGenerator();
            assemblyGenerator.ReferenceAssemblyContainingType<OpenApiClientBase>();
            assemblyGenerator.ReferenceAssemblyContainingType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>();
            assemblyGenerator.ReferenceAssemblyContainingType<System.Net.Http.HttpClient>();
            assemblyGenerator.ReferenceAssemblyContainingType<System.ComponentModel.DataAnnotations.RequiredAttribute>();
            assemblyGenerator.ReferenceAssemblyContainingType<Newtonsoft.Json.JsonConverter>();
            var clientAssembly = assemblyGenerator.Generate(clientCode);

            var result = clientAssembly.GetExportedTypes()
                .Where(x => !x.IsAbstract && x.Name.EndsWith("Api"))
                .ToList();

            return result;
        }

        private static string GetNamespace(string pluginRoute)
        {
            var capitalizedPluginRouteParts = pluginRoute
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(TrimNamespaceElement)
                .ToArray();

            return typeof(ApiFactory).Namespace + ".Generated." + string.Join(".", capitalizedPluginRouteParts);
        }

        private static string TrimNamespaceElement(string value)
        {
            var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(Capitalize)
                .ToArray();

            value = string.Join("", parts);

            return Capitalize(value);
        }

        private static string Capitalize(string value)
        {
            return value.Substring(0, 1).ToUpper() + value.Substring(1);
        }
    }
}
