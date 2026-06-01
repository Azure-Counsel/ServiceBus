public class RemediationEnvelope
{
    public string MessageId { get; set; }

    public string Body { get; set; }

    public string DeadLetterReason { get; set; }

    public int RetryCount { get; set; }

    public int DeliveryCount { get; set; }

    public DateTimeOffset FailedAt { get; set; }
}