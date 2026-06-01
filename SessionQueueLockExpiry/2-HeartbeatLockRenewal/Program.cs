using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace HeartbeatLockRenewalDemo;

public static class Program
{
    public static async Task Main()
    {
        var loggerFactory =
            LoggerFactory.Create(builder =>
                builder.AddConsole());

        var logger =
            loggerFactory.CreateLogger("Demo");

        var client =
            new ServiceBusClient(
                "<connection-string>");

        var receiver =
            await client.AcceptNextSessionAsync(
                "orders");

        logger.LogInformation(
            "Session acquired.");

        var heartbeat =
            new HeartbeatTracker();

        var renewal =
            new SessionLockHeartbeatRenewal(
                receiver,
                heartbeat,
                logger);

        var processor =
            new LongRunningOrderProcessor(
                heartbeat,
                logger);

        using var cts =
            new CancellationTokenSource();

        var renewalTask =
            renewal.RunAsync(cts.Token);

        try
        {
            await processor.ProcessAsync();
        }
        finally
        {
            cts.Cancel();
        }

        await renewalTask;
    }
}