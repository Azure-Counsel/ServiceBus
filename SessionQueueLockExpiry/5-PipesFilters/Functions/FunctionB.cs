using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PipesAndFiltersFunctionApp.Models;
using System.Text.Json;

namespace PipesAndFiltersFunctionApp.Functions;

public class FunctionB_DbStep
{
    private readonly ILogger _logger;

    public FunctionB_DbStep(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<FunctionB_DbStep>();
    }

    [Function("FunctionB_DbStep")]
    public async Task Run(
        [ServiceBusTrigger("queue-b-commits", Connection = "ServiceBusConnection", IsSessionsEnabled = true)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var body = message.Body.ToString();
        var order = JsonSerializer.Deserialize<OrderMessage>(body)!;

        _logger.LogInformation("FUNCTION B started DB write for OrderId: {OrderId}", order.OrderId);

        // Simulate DB throttling + retry-heavy workload (~28s)
        await Task.Delay(28000);

        _logger.LogInformation("DB WRITE completed for OrderId: {OrderId}", order.OrderId);

        await messageActions.CompleteMessageAsync(message);
    }
}