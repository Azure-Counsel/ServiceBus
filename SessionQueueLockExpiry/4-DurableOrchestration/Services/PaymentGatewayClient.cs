using DurableServiceBusDemo.Models;

namespace DurableServiceBusDemo.Services;

public class PaymentGatewayClient
{
    public async Task<PaymentResult> ChargeAsync(OrderMessage order)
    {
        await Task.Delay(TimeSpan.FromSeconds(45));

        return new PaymentResult
        {
            Success = true,
            TransactionId = Guid.NewGuid().ToString()
        };
    }
}