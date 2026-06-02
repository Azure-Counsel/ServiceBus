using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AsbIdempotencyDemo.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<PaymentGateway>();
        services.AddSingleton<FakeSqlDatabase>();
        services.AddSingleton<IdempotencyStore>();
    })
    .Build();

host.Run();