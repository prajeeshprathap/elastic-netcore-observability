using System;
using Inventory.Web.EventHandlers;
using Inventory.Web.Events;
using Inventory.Web.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Observability.Core;
using Observability.MongoDb;
using Observability.RabbitMq;
using Polly;
using Polly.Extensions.Http;

namespace Inventory.Web
{
    public static class ServiceCollectionExtensions
    {
        internal static void ConfigureFaultTolerantHttpClient(this IServiceCollection services)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                // Handle 404 not found
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                // Handle 401 Unauthorized
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                // What to do if any of the above erros occur:
                // Retry 3 times, each time wait 1,2 and 4 seconds before retrying.
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            services.AddHttpClient("FaultHandlingHttpClient").AddPolicyHandler(retryPolicy);
            services.AddTransient<IFaultHandlingHttpClient, FaultHandlingHttpClient>();
        }

        internal static void ConfigureRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RabbitMqConfiguration>(configuration.GetSection("RabbitMq"));
            services.AddTransient<IMessageProducer, RabbitMqMessageProducer>();
            services.AddSingleton<IMessageConsumer, RabbitMqMessageConsumer>();
        }

        internal static void ConfigureRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MongoDbConfiguration>(configuration.GetSection("MongoDb"));
            services.AddTransient<IInventoryRepository, InventoryRepository>();
        }

        internal static void ConfigureEventHandlers(this IServiceCollection services)
        {
            services.AddSingleton(new EventHandlerConfiguration()
                .RegisterConsumer<OrderProcessedEvent, OrderProcessedEventHandler>());

            services.AddTransient<OrderProcessedEventHandler>();
            services.AddTransient(typeof(EventHandlerLogger<,>));
        }

        internal static void ConfigureApiDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(swagger =>
            {
                swagger.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Orders",
                    Description = "Orders API",
                    TermsOfService = new Uri("https://www.prajeeshprathap.com"),
                    Version = "v1",
                    Contact = new OpenApiContact
                    {
                        Name = "Prajeesh Prathap",
                        Url = new Uri("https://www.prajeeshprathap.com"),
                    }
                });
            });
        }
    }
}
