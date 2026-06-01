namespace IdempotencyShieldDemo.Models;

public class ProcessedMessageStore
{
    public HashSet<string> Messages { get; set; } = new();
}