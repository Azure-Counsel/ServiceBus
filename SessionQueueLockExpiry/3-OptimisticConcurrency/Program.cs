using IdempotencyShieldDemo.Infrastructure;
using IdempotencyShieldDemo.Messaging;
using IdempotencyShieldDemo.Services;

namespace IdempotencyShieldDemo;

class Program
{
    static void Main()
    {
        var store = new InMemoryOptimisticStore();

        var accountService = new AccountService(store);
        var processor = new MessageProcessor(accountService);

        var worker = new ServiceBusWorker(processor);

        Console.WriteLine("======================================================================================");
        Console.WriteLine("🛡️ IDENTITY SHIELD - OPTIMISTIC CONCURRENCY + IDEMPOTENCY DEMO");
        Console.WriteLine("======================================================================================");

        worker.Simulate();

        Console.WriteLine("\n======================================================================================");
        Console.WriteLine("FINAL STATE:");
        var finalState = store.Read();
        Console.WriteLine($"Balance: {finalState.Balance}");
        Console.WriteLine($"ETag: {finalState.ETag}");
        Console.WriteLine($"Processed: {string.Join(",", finalState.ProcessedMessages)}");
    }
}