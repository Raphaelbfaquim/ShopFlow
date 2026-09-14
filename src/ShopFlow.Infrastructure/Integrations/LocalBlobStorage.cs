using System.Text.Json;
using ShopFlow.Application.Abstractions.Integrations;

namespace ShopFlow.Infrastructure.Integrations;

public sealed class LocalBlobStorage : IBlobStorage
{
    private readonly string _root = Path.Combine(AppContext.BaseDirectory, "storage");

    public async Task UploadJsonAsync(string container, string blobName, object payload, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(_root, container);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, blobName.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
    }
}
