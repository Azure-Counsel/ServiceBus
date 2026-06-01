using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class DlqInspectorFunction
{
    private readonly ILogger<DlqInspectorFunction> _logger;
    private readonly IsolateService _isolate;
    private readonly InspectService _inspect;
    private readonly ControlledRequeueService _requeue;
    private readonly BlobArchiveService _archive;

    public DlqInspectorFunction(
        ILogger<DlqInspectorFunction> logger,
        IsolateService isolate,
        InspectService inspect,
        ControlledRequeueService requeue,
        BlobArchiveService archive)
    {
        _logger = logger;
        _isolate = isolate;
        _inspect = inspect;
        _requeue = requeue;
        _archive = archive;
    }

    [Function("DlqInspector")]
    public async Task Run(
        [ServiceBusTrigger(
            "orders/$deadletterqueue",
            Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message)
    {
        var envelope =
            _isolate.Isolate(message);

        var inspection =
            _inspect.Inspect(envelope);

        _logger.LogInformation(
            "Message {MessageId} classified as {Type}",
            envelope.MessageId,
            inspection.FailureType);

        if (inspection.Decision == "Requeue")
        {
            await _requeue.RequeueAsync(envelope);

            _logger.LogInformation(
                "Message {MessageId} requeued",
                envelope.MessageId);

            return;
        }

        await _archive.ArchiveAsync(envelope);

        _logger.LogInformation(
            "Message {MessageId} archived",
            envelope.MessageId);
    }
}