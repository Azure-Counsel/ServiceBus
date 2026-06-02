using System.Text.Json;
using AsbHybridIdempotencyDemo.Models;
using AsbHybridIdempotencyDemo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AsbHybridIdempotencyDemo.Functions;

public class OrderProcessorFunction
{
    private readonly HybridIdempotencyService _service;
    private readonly RedisCacheService _redis;
    private readonly ILogger<OrderProcessorFunction> _logger;

    public OrderProcessorFunction(
        HybridIdempotencyService service,
        RedisCacheService redis,
        ILogger<OrderProcessorFunction> logger)
    {
        _service = service;
        _redis = redis;
        _logger = logger;
    }

    [Function("OrderProcessor")]
    public void Run(
        [ServiceBusTrigger("orders", Connection = "ServiceBusConnection")]
        string message)
    {
        var order = JsonSerializer.Deserialize<OrderMessage>(message)!;

        _logger.LogInformation($"📩 Received {order.OrderId}");

        if (Environment.GetEnvironmentVariable("SimulateCacheEviction") == "true")
        {
            _redis.SimulateEviction();
        }

        _service.Process(order);
    }
}