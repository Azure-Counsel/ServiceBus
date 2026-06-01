public static class Producer
{
    public static async Task SeedAsync(
        ServiceBusSender sender)
    {
        var messages = new[]
        {
            new LedgerMessage
            {
                Sequence = 1,
                Action = "Debit",
                Amount = 100
            },

            new LedgerMessage
            {
                Sequence = 2,
                Action = "Interest"
            },

            new LedgerMessage
            {
                Sequence = 3,
                Action = "Close"
            }
        };

        foreach (var msg in messages)
        {
            await sender.SendMessageAsync(
                new ServiceBusMessage(
                    JsonSerializer.Serialize(msg))
                {
                    SessionId = "Ledger-1"
                });
        }
    }
}