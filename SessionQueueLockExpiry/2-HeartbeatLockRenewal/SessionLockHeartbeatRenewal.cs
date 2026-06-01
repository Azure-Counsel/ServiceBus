using Azure.Messaging.ServiceBus;

namespace HeartbeatLockRenewalDemo;

public class SessionLockHeartbeatRenewal
{
    private readonly ServiceBusSessionReceiver _receiver;

    private readonly HeartbeatTracker _heartbeat;

    private readonly ILogger _logger;

    private readonly TimeSpan _heartbeatTimeout =
        TimeSpan.FromSeconds(30);

    private readonly TimeSpan _renewInterval =
        TimeSpan.FromSeconds(15);

    public SessionLockHeartbeatRenewal(
        ServiceBusSessionReceiver receiver,
        HeartbeatTracker heartbeat,
        ILogger logger)
    {
        _receiver = receiver;
        _heartbeat = heartbeat;
        _logger = logger;
    }

    public async Task RunAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(
                _renewInterval,
                cancellationToken);

            if (_heartbeat.Age >
                _heartbeatTimeout)
            {
                _logger.LogError(
                    "Heartbeat stale ({Age}s). " +
                    "Stopping lock renewal.",
                    _heartbeat.Age.TotalSeconds);

                return;
            }

            try
            {
                await _receiver
                    .RenewSessionLockAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "Session lock renewed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Renewal failed.");

                return;
            }
        }
    }
}