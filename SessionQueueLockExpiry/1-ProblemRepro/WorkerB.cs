public static class WorkerB
{
    public static async Task Run(
        ServiceBusClient client)
    {
        await Task.Delay(31000);

        var receiver =
            await client.AcceptNextSessionAsync(
                "ledger");

        Console.WriteLine(
            "WORKER B STOLE SESSION");

        while (true)
        {
            var msg =
                await receiver.ReceiveMessageAsync(
                    TimeSpan.FromSeconds(2));

            if (msg == null)
                break;

            var body =
                JsonSerializer.Deserialize<LedgerMessage>(
                    msg.Body);

            switch (body.Sequence)
            {
                case 2:

                    LedgerStore.ApplyInterest(0.02m);

                    break;

                case 3:

                    LedgerStore.Close();

                    break;
            }

            await receiver.CompleteMessageAsync(msg);
        }
    }
}