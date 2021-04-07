using Microsoft.Extensions.DependencyInjection;
using Weikio.ApiFramework.Abstractions.DependencyInjection;
using Weikio.ApiFramework.SDK;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public static class ServiceExtensions
    {
        public static IApiFrameworkBuilder AddOpenApi(this IApiFrameworkBuilder builder, string endpoint = null, ApiOptions configuration = null)
        {
            builder.Services.AddOpenApi(endpoint, configuration);

            return builder;
        }

        public static IServiceCollection AddOpenApi(this IServiceCollection services, string endpoint = null, ApiOptions configuration = null)
        {
            services.RegisterPlugin(endpoint, configuration);

            return services;
        }
    }
}
