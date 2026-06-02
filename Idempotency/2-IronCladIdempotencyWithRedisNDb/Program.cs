using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AsbHybridIdempotencyDemo.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<RedisCacheService>();
        services.AddSingleton<SqlLedgerService>();
        services.AddSingleton<PaymentGateway>();
        services.AddSingleton<HybridIdempotencyService>();
    })
    .Build();

host.Run();