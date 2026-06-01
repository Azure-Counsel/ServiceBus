using DurableServiceBusDemo.Models;
using DurableServiceBusDemo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DurableServiceBusDemo.Functions;

public class OrderActivities
{
    private readonly PaymentGatewayClient _paymentGateway;
    private readonly OrderRepository _repository;
    private readonly ILogger<OrderActivities> _logger;

    public OrderActivities(
        PaymentGatewayClient paymentGateway,
        OrderRepository repository,
        ILogger<OrderActivities> logger)
    {
        _paymentGateway = paymentGateway;
        _repository = repository;
        _logger = logger;
    }

    [Function(nameof(ValidateOrder))]
    public Task ValidateOrder(
        [ActivityTrigger] OrderMessage order)
    {
        _logger.LogInformation(
            "Validating Order {OrderId}",
            order.OrderId);

        return Task.CompletedTask;
    }

    [Function(nameof(ChargePayment))]
    public async Task<PaymentResult> ChargePayment(
        [ActivityTrigger] OrderMessage order)
    {
        _logger.LogInformation(
            "Charging payment for {OrderId}",
            order.OrderId);

        return await _paymentGateway.ChargeAsync(order);
    }

    [Function(nameof(WaitForInventory))]
    public async Task WaitForInventory(
        [ActivityTrigger] OrderMessage order)
    {
        _logger.LogInformation(
            "Waiting for inventory confirmation...");

        await Task.Delay(TimeSpan.FromSeconds(30));
    }

    [Function(nameof(PersistOrder))]
    public async Task PersistOrder(
        [ActivityTrigger]
        (OrderMessage Order, string TransactionId) input)
    {
        _logger.LogInformation(
            "Persisting order");

        await _repository.SaveAsync(
            input.Order,
            input.TransactionId);
    }
}