namespace QuickBooksAPI.API.DTOs.Response;

public class GlSettingsDto
{
    public int UserId { get; set; }
    // Thresholds
    public int ThresholdCritical { get; set; }
    public int ThresholdHigh { get; set; }
    public int ThresholdMedium { get; set; }
    public int ThresholdLow { get; set; }
    // Tier weights
    public decimal WeightTier1Statistical { get; set; }
    public decimal WeightTier2Ml { get; set; }
    public decimal WeightTier3Rules { get; set; }
    public decimal WeightTier4Llm { get; set; }
    // LLM (API keys returned as masked strings, never plaintext)
    public string LlmProvider { get; set; } = string.Empty;
    public string LlmModel { get; set; } = string.Empty;
    public bool HasApiKeyAnthropic { get; set; }
    public bool HasApiKeyOpenai { get; set; }
    public bool HasApiKeyGoogle { get; set; }
    // Tier-1 toggles
    public bool EnableBenfordLaw { get; set; }
    public bool EnableRoundNumber { get; set; }
    public bool EnableThresholdBreach { get; set; }
    public bool EnableBackdating { get; set; }
    public bool EnablePeriodEndCluster { get; set; }
    public bool EnableFraudPatterns { get; set; }
    public bool EnableNearDuplicates { get; set; }
    // Tier-2 toggles
    public bool EnableIsolationForest { get; set; }
    public bool EnableDbscan { get; set; }
    public bool EnableAssociationRule { get; set; }
    public bool EnableCopod { get; set; }
    public bool EnableEcod { get; set; }
    public bool EnableBehaviorProfiling { get; set; }
    // Alerts
    public bool EmailAlerts { get; set; }
    public string? AlertEmail { get; set; }
    public bool HasSlackWebhook { get; set; }
    // Fiscal year
    public int FiscalYearStartMonth { get; set; }
    public List<RelatedPartyDto> RelatedPartyList { get; set; } = new();
    // Scheduled runs
    public bool ScheduledEnabled { get; set; }
    public string? ScheduledFrequency { get; set; }
    public int? ScheduledDayOfWeek { get; set; }
    public int? ScheduledHour { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RelatedPartyDto
{
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
}
