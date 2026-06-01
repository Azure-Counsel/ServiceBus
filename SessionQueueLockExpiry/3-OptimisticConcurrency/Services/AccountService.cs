using IdempotencyShieldDemo.Infrastructure;
using IdempotencyShieldDemo.Models;

namespace IdempotencyShieldDemo.Services;

public class AccountService
{
    private readonly InMemoryOptimisticStore _store;

    public AccountService(InMemoryOptimisticStore store)
    {
        _store = store;
    }

    public bool ProcessPayment(string messageId, decimal amount)
    {
        var state = _store.Read();

        Console.WriteLine($"📖 READ STATE → Balance={state.Balance}, ETag={state.ETag}");

        // Idempotency check
        if (state.ProcessedMessages.Contains(messageId))
        {
            Console.WriteLine("🛑 DUPLICATE DETECTED → skipping business logic");
            return true;
        }

        Console.WriteLine("⚡ Applying business logic...");

        state.Balance -= amount;
        state.ProcessedMessages.Add(messageId);

        var success = _store.TryUpdate(state, state.ETag);

        if (!success)
        {
            Console.WriteLine("💥 412 PRECONDITION FAILED → concurrency conflict");
            return false;
        }

        Console.WriteLine("✅ WRITE SUCCESS → state updated");
        return true;
    }
}