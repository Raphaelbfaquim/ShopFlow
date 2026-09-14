using System.Text.Json;
using Azure.Storage.Blobs;
using ShopFlow.Application.Abstractions.Integrations;

namespace ShopFlow.Infrastructure.Integrations;

public sealed class AzureBlobStorage : IBlobStorage
{
    private readonly BlobServiceClient _client;

    public AzureBlobStorage(BlobServiceClient client)
    {
        _client = client;
    }

    public async Task UploadJsonAsync(string container, string blobName, object payload, CancellationToken cancellationToken = default)
    {
        var containerClient = _client.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blob = containerClient.GetBlobClient(blobName);
        var json = JsonSerializer.Serialize(payload);
        await blob.UploadAsync(BinaryData.FromString(json), overwrite: true, cancellationToken);
    }
}
