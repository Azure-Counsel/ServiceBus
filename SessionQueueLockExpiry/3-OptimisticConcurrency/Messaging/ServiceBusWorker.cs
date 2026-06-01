using IdempotencyShieldDemo.Services;

namespace IdempotencyShieldDemo.Messaging;

public class ServiceBusWorker
{
    private readonly MessageProcessor _processor;

    public ServiceBusWorker(MessageProcessor processor)
    {
        _processor = processor;
    }

    public void Simulate()
    {
        // Worker A (slow / hangs)
        Console.WriteLine("🟢 Worker A processing Msg-1");
        _processor.Process("Msg-1");

        Thread.Sleep(5000); // simulate long API call

        // Worker B (retry / lock steal simulation)
        Console.WriteLine("\n🟢 Worker B retrying Msg-1");
        _processor.Process("Msg-1");
    }
}