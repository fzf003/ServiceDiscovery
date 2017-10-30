using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;
using System.Net.Sockets;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using CondenserDotNet.Client;
using CondenserDotNet.Client.Services;
using CondenserDotNet.Configuration;

namespace ServiceDiscovery
{
    class Program
    {
        static void Main(string[] args)
        {
            var apiHost = new WebHostBuilder()
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                })
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, 5000, listenOptions =>
                    {
                        listenOptions.UseConnectionLogging();
                    });
                })
                .UseStartup<Startup>()
                .Build();

            apiHost.Run();
        }
    }

    internal class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Default");

            app.Run(async context =>
            {
                var connectionFeature = context.Connection;
                logger.LogDebug($"Peer: {connectionFeature.RemoteIpAddress?.ToString()}:{connectionFeature.RemotePort}"
                    + $"{Environment.NewLine}"
                    + $"Sock: {connectionFeature.LocalIpAddress?.ToString()}:{connectionFeature.LocalPort}");

                var response = $"hello, world";
                context.Response.ContentLength = response.Length;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(response);
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
        }
    }
}
