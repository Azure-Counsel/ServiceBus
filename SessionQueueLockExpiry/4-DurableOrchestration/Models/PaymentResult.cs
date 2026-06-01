namespace DurableServiceBusDemo.Models;

public class PaymentResult
{
    public bool Success { get; set; }

    public string TransactionId { get; set; } = string.Empty;
}