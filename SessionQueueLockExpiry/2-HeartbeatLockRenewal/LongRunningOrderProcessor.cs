namespace HeartbeatLockRenewalDemo;

public class LongRunningOrderProcessor
{
    private readonly HeartbeatTracker _heartbeat;

    private readonly ILogger _logger;

    public LongRunningOrderProcessor(
        HeartbeatTracker heartbeat,
        ILogger logger)
    {
        _heartbeat = heartbeat;
        _logger = logger;
    }

    public async Task ProcessAsync()
    {
        await ProcessChunkAsync(
            "Chunk 1",
            TimeSpan.FromSeconds(5));

        await ProcessChunkAsync(
            "Chunk 2",
            TimeSpan.FromSeconds(5));

        _logger.LogWarning(
            "Calling external API...");

        //
        // Simulate hung dependency
        //

        await Task.Delay(
            Timeout.InfiniteTimeSpan);
    }

    private async Task ProcessChunkAsync(
        string chunkName,
        TimeSpan duration)
    {
        _logger.LogInformation(
            "Processing {Chunk}",
            chunkName);

        await Task.Delay(duration);

        _heartbeat.Beat();

        _logger.LogInformation(
            "Heartbeat sent.");
    }
}