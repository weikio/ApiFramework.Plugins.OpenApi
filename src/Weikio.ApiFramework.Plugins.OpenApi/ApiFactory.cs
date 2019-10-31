﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LamarCompiler;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public static class ApiFactory
    {
        public static Task<IEnumerable<Type>> Create(string endpointRoute, ApiOptions apiOptions)
        {
            var openApiDocument = OpenApiDocument.FromUrlAsync(apiOptions.SpecificationUrl).Result;

            var clientGeneratorSettings = new CSharpClientGeneratorSettings
            {
                ClassName = "{controller}Api",
                InjectHttpClient = false,
                UseHttpClientCreationMethod = true,
                DisposeHttpClient = false,
                ClientBaseClass = typeof(OpenApiClientBase).FullName,
                ConfigurationClass = typeof(ApiOptions).FullName,
                GenerateOptionalParameters = true,
                CSharpGeneratorSettings =
                {
                    Namespace = GetNamespace(endpointRoute)
                }
            };

            var clientGenerator = new CSharpClientGenerator(openApiDocument, clientGeneratorSettings);
            var clientCode = clientGenerator.GenerateFile();

            var assemblyGenerator = new AssemblyGenerator();
            assemblyGenerator.ReferenceAssemblyContainingType<OpenApiClientBase>();
            assemblyGenerator.ReferenceAssemblyContainingType<System.Net.Http.HttpClient>();
            assemblyGenerator.ReferenceAssemblyContainingType<System.ComponentModel.DataAnnotations.RequiredAttribute>();
            assemblyGenerator.ReferenceAssemblyContainingType<Newtonsoft.Json.JsonConverter>();
            var clientAssembly = assemblyGenerator.Generate(clientCode);

            var result = clientAssembly.GetExportedTypes()
                .Where(x => !x.IsAbstract && x.Name.EndsWith("Api"))
                .ToList();

            return Task.FromResult<IEnumerable<Type>>(result);
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