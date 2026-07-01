namespace QuickBooksAPI.Application.Interfaces;

public interface IBlobStorageService
{
    /// <summary>Uploads a stream and returns the blob path (container/blobName).</summary>
    Task<string> UploadAsync(Stream content, string fileName, string containerName, CancellationToken cancellationToken = default);
}
