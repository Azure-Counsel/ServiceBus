namespace AsbHybridIdempotencyDemo.Services;

public class PaymentGateway
{
    public decimal Total { get; private set; }

    public void Charge(string orderId, decimal amount)
    {
        Total += amount;
        Console.WriteLine($"💳 Charged {orderId}: ${amount} | Total={Total}");
    }
}