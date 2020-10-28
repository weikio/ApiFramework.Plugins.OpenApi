using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSwag.Generation.Processors;
using Weikio.ApiFramework.AspNetCore;
using Weikio.ApiFramework.AspNetCore.NSwag;

namespace Weikio.ApiFramework.Plugins.OpenApi.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddRazorPages();

            services.AddApiFramework()
                .AddOpenApi("/petstore", new ApiOptions() { Mode = ApiMode.Proxy, SpecificationUrl = "https://petstore.swagger.io/v2/swagger.json" });

            services.AddTransient<IDocumentProcessor, OpenApiExtenderDocumentProcessor>();
            
            services.AddOpenApiDocument();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseOpenApi();
            app.UseSwaggerUi3();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
