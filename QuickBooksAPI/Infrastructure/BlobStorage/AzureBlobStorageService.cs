using Azure.Storage.Blobs;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Infrastructure.BlobStorage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;

    public AzureBlobStorageService(BlobServiceClient client) => _client = client;

    public async Task<string> UploadAsync(Stream content, string fileName, string containerName, CancellationToken cancellationToken = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, overwrite: false, cancellationToken: cancellationToken);

        return $"{containerName}/{blobName}";
    }
}
