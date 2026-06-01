using DurableServiceBusDemo.Models;

namespace DurableServiceBusDemo.Services;

public class OrderRepository
{
    public async Task SaveAsync(
        OrderMessage order,
        string transactionId)
    {
        await Task.Delay(TimeSpan.FromSeconds(20));

        Console.WriteLine(
            $"Saved Order {order.OrderId} with transaction {transactionId}");
    }
}