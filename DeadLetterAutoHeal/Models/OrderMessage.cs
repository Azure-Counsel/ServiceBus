namespace DlqRemediationDemo.Models;

public class OrderMessage
{
    public string OrderId { get; set; } = string.Empty;

    public string FailureType { get; set; } = string.Empty;

    public string? CustomerId { get; set; }
}