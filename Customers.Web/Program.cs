using System;
using System.Reflection;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm.SerilogEnricher;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace Customers.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings-kubernetes-customers.json", optional: true, reloadOnChange: true)
                .Build();
            var elasticUrl = config["Logging:Serilog:ElasticSearch:Url"];
            var username = config["Logging:Serilog:ElasticSearch:Username"];
            var password = config["Logging:Serilog:ElasticSearch:Password"];

            Log.Logger = new LoggerConfiguration()
                            .Enrich.FromLogContext()
                            .Enrich.WithElasticApmCorrelationInfo()
                            .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                            .WriteTo.Console()
                            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUrl)){                       
                                IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower()}",
                                //CustomFormatter = new EcsTextFormatter(),
                                ModifyConnectionSettings = x => x.BasicAuthentication(username, password),
                                EmitEventFailure =
                                            EmitEventFailureHandling.WriteToSelfLog |
                                            EmitEventFailureHandling.RaiseCallback |
                                            EmitEventFailureHandling.ThrowException,
                                FailureCallback = e => {
                                    Console.WriteLine("Failed to submit event to elastic : " + e.MessageTemplate);
                                }
                            })
                            .ReadFrom.Configuration(config)
                            .CreateLogger();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureAppConfiguration((_, config) => config
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings-kubernetes-customers.json", optional: true, reloadOnChange: true))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                }).UseAllElasticApm();
    }
}
