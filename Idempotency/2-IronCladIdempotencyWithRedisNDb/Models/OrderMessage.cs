namespace AsbHybridIdempotencyDemo.Models;

public class OrderMessage
{
    public string OrderId { get; set; } = default!;
    public decimal Amount { get; set; }
}