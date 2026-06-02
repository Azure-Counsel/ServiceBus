namespace AsbHybridIdempotencyDemo.Services;

public class SqlLedgerService
{
    private readonly HashSet<string> _ledger = new();

    public bool Exists(string orderId)
    {
        return _ledger.Contains(orderId);
    }

    public void Insert(string orderId)
    {
        if (_ledger.Contains(orderId))
        {
            throw new Exception("SQL UNIQUE constraint violation");
        }

        _ledger.Add(orderId);
        Console.WriteLine($"🗄️ SQL INSERT: {orderId}");
    }
}