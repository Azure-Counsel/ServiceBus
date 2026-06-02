namespace AsbIdempotencyDemo.Services;

public class FakeSqlDatabase
{
    private readonly HashSet<string> _orders = new();

    public bool IsAvailable { get; set; } = true;

    public void Save(string orderId)
    {
        if (!IsAvailable)
            throw new Exception("SQL Connection Pool Exhausted");

        _orders.Add(orderId);

        Console.WriteLine($"🗄️ Saved {orderId}");
    }
}