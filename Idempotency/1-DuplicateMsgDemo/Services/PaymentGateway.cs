namespace AsbIdempotencyDemo.Services;

public class PaymentGateway
{
    public decimal TotalCharged { get; private set; }

    public void Charge(string orderId, decimal amount)
    {
        TotalCharged += amount;

        Console.WriteLine($"💳 Charged {orderId}: ${amount} | Total=${TotalCharged}");
    }
}