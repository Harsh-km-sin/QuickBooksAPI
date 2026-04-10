namespace QuickBooksShared.Messages;

/// <summary>
/// Service Bus payload for full-company QuickBooks sync. Versioned for API/worker contract evolution.
/// </summary>
public class FullSyncMessage
{
    /// <summary>Schema version for this payload shape. Current: 1.</summary>
    public int SchemaVersion { get; set; } = 1;

    public string CompanyId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    /// <summary>Optional; propagated to sync scope for end-to-end tracing.</summary>
    public string? CorrelationId { get; set; }
}
