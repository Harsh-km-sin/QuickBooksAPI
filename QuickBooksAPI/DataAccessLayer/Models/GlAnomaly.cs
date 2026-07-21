namespace QuickBooksAPI.DataAccessLayer.Models;

public class GlAnomaly
{
    public int Id { get; set; }
    public int EntryId { get; set; }
    public int RunId { get; set; }
    public int UserId { get; set; }
    public string AnomalyType { get; set; } = string.Empty; // e.g. "z_score_outlier"
    public double DetectorScore { get; set; }                // 0.0–1.0 raw signal
    public string? RiskReasons { get; set; }                 // JSON string[]
    public string? Metadata { get; set; }                    // JSON evidence object
}
