namespace QuickBooksShared.Messages;

/// <summary>
/// Service Bus payload enqueued by .NET API after a GL file is uploaded; consumed by the Python analysis worker.
/// </summary>
public class GlAnalysisMessage
{
    public int SchemaVersion { get; set; } = 1;
    public int RunId { get; set; }
    public int UserId { get; set; }
    public string BlobPath { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public DateTime RequestedAt { get; set; }
}
