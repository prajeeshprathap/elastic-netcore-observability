using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.NetCoreAll;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Observability.Core;

namespace Orders.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureRabbitMq(Configuration);
            services.ConfigureEventHandlers();
            services.ConfigureFaultTolerantHttpClient();
            services.ConfigureExternalApis(Configuration);
            services.ConfigureApiDocumentation();
            services.ConfigureRepositories(Configuration);
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAllElasticApm(Configuration);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders");
                c.RoutePrefix = string.Empty;
            });

            var tracer = app.ApplicationServices.GetService<ITracer>();
            var consumer = app.ApplicationServices.GetService<IMessageConsumer>();
            consumer.RegisterOnMessageHandlerAndReceiveMessages();
        }
    }
}
