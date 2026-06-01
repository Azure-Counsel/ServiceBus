public class InspectService
{
    private const int MaxRetries = 3;

    public InspectionResult Inspect(
        RemediationEnvelope envelope)
    {
        var result = new InspectionResult();

        if (envelope.Body.Contains("INVALID"))
        {
            result.FailureType = FailureType.Poison;
            result.RetryAllowed = false;
            result.Decision = "Archive";

            return result;
        }

        if (envelope.DeadLetterReason.Contains("429"))
        {
            result.FailureType = FailureType.Transient;
            result.RetryAllowed =
                envelope.RetryCount < MaxRetries;

            result.Decision =
                result.RetryAllowed
                    ? "Requeue"
                    : "Archive";

            return result;
        }

        if (envelope.DeadLetterReason.Contains("Timeout"))
        {
            result.FailureType = FailureType.Transient;
            result.RetryAllowed =
                envelope.RetryCount < MaxRetries;

            result.Decision =
                result.RetryAllowed
                    ? "Requeue"
                    : "Archive";

            return result;
        }

        result.FailureType = FailureType.Unknown;
        result.RetryAllowed = false;
        result.Decision = "Archive";

        return result;
    }
}