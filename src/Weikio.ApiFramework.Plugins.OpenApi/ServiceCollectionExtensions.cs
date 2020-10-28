using Microsoft.Extensions.DependencyInjection;
using Weikio.ApiFramework.Abstractions.DependencyInjection;
using Weikio.ApiFramework.SDK;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public static class ServiceExtensions
    {
        public static IApiFrameworkBuilder AddOpenApi(this IApiFrameworkBuilder builder)
        {
            var assembly = typeof(ApiOptions).Assembly;
            var apiPlugin = new ApiPlugin { Assembly = assembly };

            builder.Services.AddSingleton(typeof(ApiPlugin), apiPlugin);

            builder.Services.Configure<ApiPluginOptions>(options =>
            {
                if (options.ApiPluginAssemblies.Contains(assembly))
                {
                    return;
                }

                options.ApiPluginAssemblies.Add(assembly);
            });

            return builder;
        }

        public static IApiFrameworkBuilder AddOpenApi(this IApiFrameworkBuilder builder, string endpoint, ApiOptions configuration)
        {
            builder.AddOpenApi();

            builder.Services.RegisterEndpoint(endpoint, "Weikio.ApiFramework.Plugins.OpenApi", configuration);

            return builder;
        }
    }
}
