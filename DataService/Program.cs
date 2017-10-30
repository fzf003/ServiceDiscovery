using System;
using System.Net;
using System.Linq;
using System.Net.Sockets;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using CondenserDotNet.Client;
using CondenserDotNet.Configuration;

namespace ServiceDiscovery
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = ServiceManagerConfig.GetNextAvailablePort();

            var serviceName = "Service.Data";
            var serviceId = $"{serviceName}_127.0.0.1:{port}";

            var apiHost = new WebHostBuilder()
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                })
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, port, listenOptions =>
                    {
                        listenOptions.UseConnectionLogging();
                    });
                })
                .ConfigureServices(services => 
                {
                    services.Configure<ServiceManagerConfig>(options =>
                    {
                        options.ServicePort = port;
                        options.ServiceName = serviceName;
                        options.ServiceId = serviceId;
                        options.ServiceAddress = "127.0.0.1";
                    });
                    services.AddSingleton<IConfigurationRegistry>(CondenserConfigBuilder.FromConsul().WithAgentAddress("127.0.0.1").WithAgentPort(8500).Build());
                })
                .UseStartup<Startup>()
                .Build();

            apiHost.Run();
        }
    }

    internal class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IServiceManager manager)
        {
            var logger = loggerFactory.CreateLogger("Default");

            manager.WithDeregisterIfCriticalAfter(TimeSpan.FromSeconds(30));
            manager.AddHttpHealthCheck("health", 10).RegisterServiceAsync();

            app.Run(async context =>
            {
                var connectionFeature = context.Connection;
                logger.LogDebug($"Peer: {connectionFeature.RemoteIpAddress?.ToString()}:{connectionFeature.RemotePort}"
                    + $"{Environment.NewLine}"
                    + $"Sock: {connectionFeature.LocalIpAddress?.ToString()}:{connectionFeature.LocalPort}");

                var response = $"hello, world from {manager.ServiceName} @ {manager.ServiceAddress}:{manager.ServicePort}";
                context.Response.ContentLength = response.Length;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(response);
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddConsulServices();
        }
    }
}
