using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Infrastructure.BlobStorage;

/// <summary>Fallback for local dev — saves uploaded GL files to the system temp folder.</summary>
public class LocalFileBlobStorageService : IBlobStorageService
{
    public async Task<string> UploadAsync(Stream content, string fileName, string containerName, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(Path.GetTempPath(), "gl-uploads", containerName);
        Directory.CreateDirectory(dir);

        var blobName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(dir, blobName);

        using var fs = File.Create(filePath);
        await content.CopyToAsync(fs, cancellationToken);

        return $"{containerName}/{blobName}";
    }
}
