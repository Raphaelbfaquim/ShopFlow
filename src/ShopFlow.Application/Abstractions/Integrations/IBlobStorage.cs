namespace ShopFlow.Application.Abstractions.Integrations;

public interface IBlobStorage
{
    Task UploadJsonAsync(string container, string blobName, object payload, CancellationToken cancellationToken = default);
}
