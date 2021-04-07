using System;
using NJsonSchema.CodeGeneration;
using NSwag.CodeGeneration.CSharp.Models;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    /// <summary>
    /// Documentation template is used to inject HTTP method attributes after
    /// the documentation part.
    /// </summary>
    public class ClientMethodDocumentationTemplate : ITemplate
    {
        private readonly ITemplate _defaultTemplate;
        private readonly CSharpOperationModel _operationModel;

        public ClientMethodDocumentationTemplate(ITemplate defaultTemplate, CSharpOperationModel operationModel)
        {
            _defaultTemplate = defaultTemplate;
            _operationModel = operationModel;
        }

        public string Render()
        {
            // render the documentation
            var renderedTemplate = _defaultTemplate.Render();
            
            // add HTTP method attribute
            renderedTemplate += Environment.NewLine + $"[Microsoft.AspNetCore.Mvc.Http{_operationModel.HttpMethodUpper}]";

            return renderedTemplate;
        }
    }
}