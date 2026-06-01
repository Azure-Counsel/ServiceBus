using System.Text.Json;
using DurableServiceBusDemo.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace DurableServiceBusDemo.Functions;

public class ServiceBusStarterFunction
{
    private readonly DurableTaskClient _durableClient;
    private readonly ILogger<ServiceBusStarterFunction> _logger;

    public ServiceBusStarterFunction(
        DurableTaskClient durableClient,
        ILogger<ServiceBusStarterFunction> logger)
    {
        _durableClient = durableClient;
        _logger = logger;
    }

    [Function(nameof(ServiceBusStarterFunction))]
    public async Task Run(
        [ServiceBusTrigger(
            queueName: "orders",
            Connection = "ServiceBusConnection")]
        string message)
    {
        var order =
            JsonSerializer.Deserialize<OrderMessage>(message)!;

        string instanceId =
            $"order-{order.SessionId}";

        _logger.LogInformation(
            "Starting orchestration {InstanceId}",
            instanceId);

        await _durableClient.ScheduleNewOrchestrationInstanceAsync(
            nameof(OrderOrchestrator),
            order,
            new StartOrchestrationOptions
            {
                InstanceId = instanceId
            });

        _logger.LogInformation(
            "Orchestration started successfully");
    }
}