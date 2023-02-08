using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag.Commands;
using NSwag.Commands.CodeGeneration;
using NSwag.Commands.Generation;
using Weikio.ApiFramework.Plugins.OpenApi.Proxy;
using Weikio.TypeGenerator;

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

            var ns = GetNamespace(endpointRoute);
            var request = new OpenApiClientAssemblyGenerationRequest()
            {
                JsonSpec = configuration.SpecificationUrl,
                OperationGenerationModes = new List<OperationGenerationMode>()
                {
                    OperationGenerationMode.MultipleClientsFromOperationId,
                    OperationGenerationMode.SingleClientFromOperationId,
                    OperationGenerationMode.MultipleClientsFromPathSegments,
                    OperationGenerationMode.SingleClientFromPathSegments,
                    OperationGenerationMode.MultipleClientsFromFirstTagAndOperationId,
                    OperationGenerationMode.MultipleClientsFromFirstTagAndPathSegments
                },
                ClassName = "{controller}Api",
                Namespace = ns,
                GenerateBaseAddress = true
            };

            var codeFile = await GenerateAssembly(request);
            var assembly = Assembly.LoadFrom(codeFile);
            var result = assembly.GetExportedTypes().Where(x => !x.IsAbstract && x.Name.EndsWith("Api"));

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

        private static async Task<string> GenerateAssembly(OpenApiClientAssemblyGenerationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.JsonSpec))
            {
                throw new ArgumentNullException(nameof(request.JsonSpec));
            }

            if (request.OperationGenerationModes?.Any() != true)
            {
                request.OperationGenerationModes = new List<OperationGenerationMode>()
                {
                    OperationGenerationMode.MultipleClientsFromOperationId,
                    OperationGenerationMode.MultipleClientsFromPathSegments,
                    OperationGenerationMode.MultipleClientsFromFirstTagAndOperationId,
                    OperationGenerationMode.MultipleClientsFromFirstTagAndPathSegments,
                    OperationGenerationMode.SingleClientFromOperationId,
                    OperationGenerationMode.SingleClientFromPathSegments
                };
            }

            if (string.IsNullOrWhiteSpace(request.ClassName))
            {
                request.ClassName = "{controller}Client";
            }

            var generationPath = Path.Combine(Path.GetTempPath(), "openapiclientgen");

            Directory.CreateDirectory(generationPath);

            var assemblyGenerator = new CodeToAssemblyGenerator(workingFolder: generationPath);
            assemblyGenerator.ReferenceAssemblyContainingType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>();
            assemblyGenerator.ReferenceAssemblyContainingType<HttpClient>();
            assemblyGenerator.ReferenceAssemblyContainingType<System.ComponentModel.DataAnnotations.RequiredAttribute>();
            assemblyGenerator.ReferenceAssemblyContainingType<Newtonsoft.Json.JsonConverter>();
            assemblyGenerator.ReferenceAssemblyContainingType<OpenApiClientBase>();
            assemblyGenerator.ReferenceAssemblyContainingType<Microsoft.AspNetCore.Mvc.FromBodyAttribute>();

            // We prefer MultipleClientsFromOperationId but try every possible way if NSwag happens to generate invalid code
            foreach (var generationMode in request.OperationGenerationModes)
            {
                var generatedCode = await GenerateCode(request, generationMode, generationPath);

                try
                {
                    var result = assemblyGenerator.GenerateAssembly(generatedCode);

                    if (result == null)
                    {
                        throw new Exception("Code generation was successful but assembly is missing");
                    }

                    if (string.IsNullOrWhiteSpace(result.Location))
                    {
                        throw new Exception("Code generation was successful but assembly location is missing");
                    }

                    try
                    {
                        if (string.IsNullOrWhiteSpace(request.OutputAssemblyFilePath))
                        {
                            return result.Location;
                        }

                        File.Copy(result.Location, request.OutputAssemblyFilePath);

                        return request.OutputAssemblyFilePath;

                    }
                    catch (Exception e)
                    {
                        throw new Exception("Couldn't copy generated assembly to requested AssemblyFilePath", e);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to generate assembly from code", e);
                }
            }

            throw new Exception("Couldn't generate assembly from OpenAPI specification");
        }

        private static async Task<string> GenerateCode(OpenApiClientAssemblyGenerationRequest request, OperationGenerationMode operationGenerationMode,
            string generationPath)
        {
            var tempOutputfilePath = Path.Combine(generationPath, Path.GetRandomFileName());

            var document = NSwagDocument.Create();
            document.Runtime = Enum.Parse<Runtime>(request.Runtime ?? "NetCore31");

            document.CodeGenerators.OpenApiToCSharpClientCommand = new OpenApiToCSharpClientCommand()
            {
                ClientBaseClass = typeof(OpenApiClientBase).FullName,
                Namespace = request.Namespace,
                ClassStyle = CSharpClassStyle.Poco,
                JsonLibrary = CSharpJsonLibrary.NewtonsoftJson,
                GenerateClientClasses = true,
                GenerateClientInterfaces = false,
                GenerateSyncMethods = false,
                OutputFilePath = tempOutputfilePath,
                ClassName = request.ClassName,
                OperationGenerationMode = operationGenerationMode,
                GenerateOptionalParameters = true,
                DateTimeType = "System.DateTime",
                DateType = "System.DateTime",
                GenerateResponseClasses = false,
                GenerateJsonMethods = false,
                ResponseArrayType = "System.Collections.Generic.List",
                UseBaseUrl = false,
                GenerateBaseUrlProperty = false,
                InjectHttpClient = false,
            };

            if (request.GenerateBaseAddress)
            {
                document.CodeGenerators.OpenApiToCSharpClientCommand.UseBaseUrl = true;
                document.CodeGenerators.OpenApiToCSharpClientCommand.GenerateBaseUrlProperty = true;
            }

            document.SelectedSwaggerGenerator = new FromDocumentCommand() { Json = request.JsonSpec, };

            try
            {
                await document.ExecuteAsync();
            }
            catch (Exception e)
            {
                throw new Exception("Couldn't execute NSwag", e);
            }

            if (System.IO.File.Exists(tempOutputfilePath) == false)
            {
                throw new Exception("Couldn't locate generated code file");
            }

            var generatedCode = await System.IO.File.ReadAllTextAsync(tempOutputfilePath);

            if (string.IsNullOrWhiteSpace(generatedCode))
            {
                throw new Exception("Generated code file is empty, make sure provided Swagger is valid");
            }

            return generatedCode;
        }
    }
}
