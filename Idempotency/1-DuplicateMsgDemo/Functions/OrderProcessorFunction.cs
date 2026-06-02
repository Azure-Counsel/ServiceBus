using System.Text.Json;
using AsbIdempotencyDemo.Models;
using AsbIdempotencyDemo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AsbIdempotencyDemo.Functions;

public class OrderProcessorFunction
{
    private readonly PaymentGateway _payment;
    private readonly FakeSqlDatabase _db;
    private readonly IdempotencyStore _idempotency;
    private readonly ILogger<OrderProcessorFunction> _logger;

    public OrderProcessorFunction(
        PaymentGateway payment,
        FakeSqlDatabase db,
        IdempotencyStore idempotency,
        ILogger<OrderProcessorFunction> logger)
    {
        _payment = payment;
        _db = db;
        _idempotency = idempotency;
        _logger = logger;
    }

    [Function("OrderProcessor")]
    public void Run(
        [ServiceBusTrigger("orders", Connection = "ServiceBusConnection")]
        string message,
        FunctionContext context)
    {
        var order = JsonSerializer.Deserialize<OrderMessage>(message)!;

        var now = DateTime.UtcNow;

        _logger.LogInformation($"📩 Received {order.OrderId}");

        // STEP 1: simulate duplicate detection (BROKER LEVEL IS NOT IDENTITY SAFETY)
        if (_idempotency.IsDuplicate(order.OrderId, now))
        {
            _logger.LogWarning($"🚫 Duplicate detected in app layer: {order.OrderId}");
            return;
        }

        try
        {
            // STEP 2: payment happens first (side-effect)
            _payment.Charge(order.OrderId, order.Amount);

            // STEP 3: simulate outage toggle
            if (Environment.GetEnvironmentVariable("SimulateFailureMode") == "true")
            {
                _db.IsAvailable = false;
            }

            // STEP 4: DB write fails
            _db.Save(order.OrderId);

            _logger.LogInformation($"✅ Completed {order.OrderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"💥 Failure: {ex.Message}");
            throw; // causes Service Bus retry
        }
    }
}