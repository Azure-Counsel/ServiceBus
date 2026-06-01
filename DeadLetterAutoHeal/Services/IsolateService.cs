using Azure.Messaging.ServiceBus;

public class IsolateService
{
    public RemediationEnvelope Isolate(
        ServiceBusReceivedMessage message)
    {
        int retryCount = 0;

        if (message.ApplicationProperties.TryGetValue(
            HeaderKeys.RemediationRetryCount,
            out var retry))
        {
            retryCount = Convert.ToInt32(retry);
        }

        return new RemediationEnvelope
        {
            MessageId = message.MessageId,
            Body = message.Body.ToString(),
            DeadLetterReason = message.DeadLetterReason,
            RetryCount = retryCount,
            DeliveryCount = message.DeliveryCount,
            FailedAt = DateTimeOffset.UtcNow
        };
    }
}