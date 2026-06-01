public static class WorkerA
{
    public static async Task Run(
        ServiceBusClient client)
    {
        var receiver =
            await client.AcceptNextSessionAsync(
                "ledger");

        Console.WriteLine(
            "WORKER A ACQUIRED SESSION");

        var msg =
            await receiver.ReceiveMessageAsync();

        var body =
            JsonSerializer.Deserialize<LedgerMessage>(
                msg.Body);

        Console.WriteLine(
            "M1 RECEIVED");

        await Task.Delay(40000);

        Console.WriteLine(
            "DB RETRY FINALLY SUCCEEDED");

        LedgerStore.ApplyDebit(100);

        try
        {
            await receiver.CompleteMessageAsync(msg);
        }
        catch
        {
            Console.WriteLine(
                "Lock already expired");
        }
    }
}