using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Messaging.ServiceBus;
using PipesAndFiltersFunctionApp.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ServiceBusClient>(sp =>
        {
            var connection = Environment.GetEnvironmentVariable("ServiceBusConnection");
            return new ServiceBusClient(connection);
        });

        services.AddSingleton<FakeApiClient>();
    })
    .Build();

host.Run();