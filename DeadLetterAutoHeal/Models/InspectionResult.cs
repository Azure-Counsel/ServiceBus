namespace DlqRemediationDemo.Models;

public class InspectionResult
{
    /// <summary>
    /// Classification of the failure.
    /// </summary>
    public FailureType FailureType { get; set; }

    /// <summary>
    /// Whether the message can be retried.
    /// </summary>
    public bool RetryAllowed { get; set; }

    /// <summary>
    /// Requeue, Archive, Escalate.
    /// </summary>
    public string Decision { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable explanation.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Current remediation retry count.
    /// </summary>
    public int CurrentRetryCount { get; set; }

    /// <summary>
    /// Maximum retries allowed.
    /// </summary>
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// Whether the message should be archived.
    /// </summary>
    public bool ArchiveRequired { get; set; }

    /// <summary>
    /// Whether the message should be requeued.
    /// </summary>
    public bool RequeueRequired { get; set; }

    /// <summary>
    /// When the inspection occurred.
    /// </summary>
    public DateTimeOffset InspectedAt { get; set; }
        = DateTimeOffset.UtcNow;
}