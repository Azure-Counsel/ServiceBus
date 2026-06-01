using DurableServiceBusDemo.Models;
using Microsoft.DurableTask;

namespace DurableServiceBusDemo.Functions;

public class OrderOrchestrator
{
    [Function(nameof(OrderOrchestrator))]
    public async Task Run(
        [OrchestrationTrigger]
        TaskOrchestrationContext context)
    {
        var order =
            context.GetInput<OrderMessage>()!;

        await context.CallActivityAsync(
            nameof(OrderActivities.ValidateOrder),
            order);

        var paymentResult =
            await context.CallActivityAsync<PaymentResult>(
                nameof(OrderActivities.ChargePayment),
                order);

        await context.CallActivityAsync(
            nameof(OrderActivities.WaitForInventory),
            order);

        await context.CallActivityAsync(
            nameof(OrderActivities.PersistOrder),
            (order, paymentResult.TransactionId));
    }
}