namespace IdempotencyShieldDemo.Models;

public class BankAccount
{
    public decimal Balance { get; set; }
    public string ETag { get; set; } = "V-1";

    public HashSet<string> ProcessedMessages { get; set; } = new();
}