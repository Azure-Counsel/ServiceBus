namespace DurableServiceBusDemo.Models;

public class OrderMessage
{
    public string OrderId { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string CustomerId { get; set; } = string.Empty;
}