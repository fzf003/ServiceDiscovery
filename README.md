# Server-Side Service Discovery

Mini workshop using <https://github.com/fabiolb/fabio>, <https://github.com/hashicorp/consul>, <https://github.com/Drawaes/CondenserDotNet> and <https://github.com/dotnet/core>

The Workshop aims to implement a simple API which registers itself with Consul. The API uses Fabio supported tags.

## Part 0

Run solution and verify that <http://localhost:5000> returns "hello, world"

## Part 1

Add this to the Main function:

```C#
var port = ServiceManagerConfig.GetNextAvailablePort();
var serviceName = "Service.Api";
var serviceId = $"{serviceName}_127.0.0.1:{port}";
```

Replace kestrel config 

```C#
options.Listen(IPAddress.Any, 5000, listenOptions =>
```

with

```C#
options.Listen(IPAddress.Any, port, listenOptions => 
```

Add this to the kestrel config:

```C#
.ConfigureServices(
    services =>
        {
            services.Configure<ServiceManagerConfig>(
                options =>
                    {
                        options.ServicePort = port;
                        options.ServiceName = serviceName;
                        options.ServiceId = serviceId;
                        options.ServiceAddress = "127.0.0.1";
                    });
            services.AddSingleton<IConfigurationRegistry>(
                CondenserConfigBuilder.FromConsul().WithAgentAddress("127.0.0.1").WithAgentPort(8500).Build());
        })
```

Modify Startup Configure method signature with this

```C#
, IServiceManager manager, IServiceRegistry serviceRegistry
```

Add this to the begining of the startup configure method:

```C#
manager.AddApiUrl("/api/ strip=/api/");
manager.WithDeregisterIfCriticalAfter(TimeSpan.FromSeconds(30));
manager.AddHttpHealthCheck("health", 10).RegisterServiceAsync();
```

Add this to the start ConfigureServices method:

```C#
services.AddConsulServices();
```

Modify the app.run response with this:

```C#
var response = $"hello, world from {manager.ServiceName} @ {manager.ServiceAddress}:{manager.ServicePort}";
```

1. Run Consul (with commandline arguments "agent -dev -ui").
2. go to <http://localhost:8500> and verify consul is running (keep open)
3. Run Fabio (no commandline arugments)
4. go to <http://localhost:9998> and verify fabio is running (keep open)
5. verify fabio is registered with consul
6. Run a few instances of ServiceDiscovery.exe
7. go to Consul and verify they are registered
8. go to Fabio and verify they are registered
9. go to <http://localhost:9999/api/> and verify that you can hit all instance [just reload it and watch the port change :-)]
10. close all ServiceDiscovery.exe and let Consul and Fabio run
11. Go to part 2.

## Part 2

Replace ServiceDiscovery app.run response with this:

```C#
var serviceData = await serviceRegistry.GetServiceInstanceAsync("Service.Data");
var response = $"hello, world from {manager.ServiceName} @ {manager.ServiceAddress}:{manager.ServicePort} using Service.Data: {serviceData.ID}";
```

1. Reload DataService
2. Run a few instances of DataService
3. Verify in Consul that they are registered
4. Run a few instances of ServiceDiscovery.exe
5. go to http://localhost:9999/api/ and see the response and how the ports rotate.