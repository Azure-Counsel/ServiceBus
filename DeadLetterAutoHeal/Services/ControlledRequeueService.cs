using Azure.Messaging.ServiceBus;

public class ControlledRequeueService
{
    private readonly ServiceBusClient _client;

    public ControlledRequeueService(
        ServiceBusClient client)
    {
        _client = client;
    }

    public async Task RequeueAsync(
        RemediationEnvelope envelope)
    {
        var sender =
            _client.CreateSender("orders");

        var message =
            new ServiceBusMessage(envelope.Body);

        message.MessageId =
            envelope.MessageId;

        message.ApplicationProperties[
            HeaderKeys.RemediationRetryCount]
                = envelope.RetryCount + 1;

        await sender.SendMessageAsync(message);
    }
}