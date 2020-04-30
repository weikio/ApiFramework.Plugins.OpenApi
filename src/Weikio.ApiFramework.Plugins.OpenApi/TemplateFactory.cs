using System;
using System.Collections.Generic;
using System.Text;
using NJsonSchema.CodeGeneration;
using NSwag.CodeGeneration.CSharp.Models;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    /// <summary>
    /// Decorator class for the default template factory.
    /// Modifies some existing templates.
    /// </summary>
    public class TemplateFactory : ITemplateFactory
    {
        private readonly ITemplateFactory _defaultTemplateFactory;

        public TemplateFactory(ITemplateFactory defaultTemplateFactory)
        {
            _defaultTemplateFactory = defaultTemplateFactory;
        }

        public ITemplate CreateTemplate(string language, string templateName, object model)
        {
            var template = _defaultTemplateFactory.CreateTemplate(language, templateName, model);

            if (templateName == "Client.Method.Documentation")
            {
                var operationModel = GetCSharpOperationModel(model);

                if (operationModel == null)
                {
                    throw new ArgumentException("Provided template model doesn't contain CSharpOperationModel object.", nameof(model));
                }

                template = new ClientMethodDocumentationTemplate(template, operationModel);
            }

            return template;
        }

        /// <summary>
        /// Tries to get CSharpOperationModel object from the generic template model object.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private CSharpOperationModel GetCSharpOperationModel(object model)
        {
            switch (model) 
            {
                case CSharpOperationModel operationModel:
                    // at some point in the future, NSwag will provide a direct access to CSharpOperationModel
                    return operationModel;

                case DotLiquid.Hash modelValues:
                    // currently, model object contains LiquidProxyHash object which inherits from DotLiquid.Hash
                    // and there should be entry for "operation" key.

                    if (modelValues.TryGetValue("operation", out var operation))
                    {
                        // operation is again a LiquidProxyHash object, which is internal and because of that,
                        // we have to access Object property using reflection.
                        var objectProperty = operation.GetType().GetProperty("Object");
                        return objectProperty?.GetValue(operation) as CSharpOperationModel;
                    }
                    break;
            }

            return null;
        }
    }

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
