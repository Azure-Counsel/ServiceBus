using Azure.Storage.Blobs;
using System.Text;

public class BlobArchiveService
{
    private readonly BlobContainerClient _container;

    public BlobArchiveService(
        BlobServiceClient client)
    {
        _container =
            client.GetBlobContainerClient(
                "deadletter-archive");
    }

    public async Task ArchiveAsync(
        RemediationEnvelope envelope)
    {
        var blobName =
            $"{envelope.MessageId}.json";

        var blob =
            _container.GetBlobClient(blobName);

        var json =
            JsonSerializer.Serialize(envelope);

        using var stream =
            new MemoryStream(
                Encoding.UTF8.GetBytes(json));

        await blob.UploadAsync(
            stream,
            overwrite: true);
    }
}