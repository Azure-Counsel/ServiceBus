using IdempotencyShieldDemo.Models;

namespace IdempotencyShieldDemo.Infrastructure;

public class InMemoryOptimisticStore
{
    private BankAccount _state = new()
    {
        Balance = 1000,
        ETag = "V-1"
    };

    private readonly object _lock = new();

    public BankAccount Read()
    {
        lock (_lock)
        {
            return new BankAccount
            {
                Balance = _state.Balance,
                ETag = _state.ETag,
                ProcessedMessages = new HashSet<string>(_state.ProcessedMessages)
            };
        }
    }

    public bool TryUpdate(BankAccount updated, string expectedETag)
    {
        lock (_lock)
        {
            if (_state.ETag != expectedETag)
                return false;

            _state.Balance = updated.Balance;
            _state.ProcessedMessages = updated.ProcessedMessages;
            _state.ETag = $"V-{int.Parse(_state.ETag.Split('-')[1]) + 1}";

            return true;
        }
    }
}