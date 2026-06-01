using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PipesAndFiltersFunctionApp.Models;
using PipesAndFiltersFunctionApp.Services;
using System.Text.Json;

namespace PipesAndFiltersFunctionApp.Functions;

public class FunctionA_ApiStep
{
    private readonly ILogger _logger;
    private readonly FakeApiClient _apiClient;
    private readonly ServiceBusClient _serviceBusClient;

    public FunctionA_ApiStep(ILoggerFactory loggerFactory, FakeApiClient apiClient, ServiceBusClient serviceBusClient)
    {
        _logger = loggerFactory.CreateLogger<FunctionA_ApiStep>();
        _apiClient = apiClient;
        _serviceBusClient = serviceBusClient;
    }

    [Function("FunctionA_ApiStep")]
    public async Task Run(
        [ServiceBusTrigger("queue-a-orders", Connection = "ServiceBusConnection", IsSessionsEnabled = true)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var body = message.Body.ToString();
        var order = JsonSerializer.Deserialize<OrderMessage>(body)!;

        _logger.LogInformation("FUNCTION A started for OrderId: {OrderId}", order.OrderId);

        // STEP 1: API CALL (flaky/slow step isolated here)
        var apiResult = await _apiClient.CallExternalApiAsync(order.Payload);

        _logger.LogInformation("API completed for OrderId: {OrderId}", order.OrderId);

        // STEP 2: forward to Queue B
        var continuation = new OrderMessage
        {
            OrderId = order.OrderId,
            Step = "DB_WRITE",
            Payload = apiResult
        };

        var sender = _serviceBusClient.CreateSender("queue-b-commits");

        var outbound = new ServiceBusMessage(JsonSerializer.Serialize(continuation))
        {
            SessionId = order.OrderId // preserves FIFO ordering downstream
        };

        await sender.SendMessageAsync(outbound);

        _logger.LogInformation("Forwarded OrderId {OrderId} to Queue B", order.OrderId);

        await messageActions.CompleteMessageAsync(message);
    }
}