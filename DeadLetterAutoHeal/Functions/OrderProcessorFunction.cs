using System.Text.Json;
using Azure.Messaging.ServiceBus;
using DlqRemediationDemo.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DlqRemediationDemo.Functions;

public class OrderProcessorFunction
{
    private readonly ILogger<OrderProcessorFunction> _logger;

    public OrderProcessorFunction(
        ILogger<OrderProcessorFunction> logger)
    {
        _logger = logger;
    }

    [Function("OrderProcessor")]
    public async Task Run(
        [ServiceBusTrigger(
            "orders",
            Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message)
    {
        var body = message.Body.ToString();

        var order = JsonSerializer.Deserialize<OrderMessage>(body);

        if (order == null)
        {
            throw new InvalidOperationException(
                "Unable to deserialize order.");
        }

        _logger.LogInformation(
            "Processing Order {OrderId}",
            order.OrderId);

        switch (order.FailureType)
        {
            case "Timeout":

                _logger.LogWarning(
                    "Simulating downstream timeout for {OrderId}",
                    order.OrderId);

                throw new TimeoutException(
                    "Inventory API timeout");

            case "429":

                _logger.LogWarning(
                    "Simulating downstream rate limit for {OrderId}",
                    order.OrderId);

                throw new HttpRequestException(
                    "429 Too Many Requests");

            case "Poison":

                _logger.LogWarning(
                    "Simulating poison message for {OrderId}",
                    order.OrderId);

                throw new InvalidDataException(
                    "Customer payload invalid");

            default:

                _logger.LogInformation(
                    "Order {OrderId} processed successfully",
                    order.OrderId);

                break;
        }

        await Task.CompletedTask;
    }
}