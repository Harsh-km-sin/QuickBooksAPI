namespace QuickBooksAPI.API.DTOs.Request;

public class GlSettingsRequest
{
    // All fields nullable — only provided fields are applied (partial update)
    public int? ThresholdCritical { get; set; }
    public int? ThresholdHigh { get; set; }
    public int? ThresholdMedium { get; set; }
    public int? ThresholdLow { get; set; }
    public decimal? WeightTier1Statistical { get; set; }
    public decimal? WeightTier2Ml { get; set; }
    public decimal? WeightTier3Rules { get; set; }
    public decimal? WeightTier4Llm { get; set; }
    public string? LlmProvider { get; set; }
    public string? LlmModel { get; set; }
    public string? ApiKeyAnthropic { get; set; }
    public string? ApiKeyOpenai { get; set; }
    public string? ApiKeyGoogle { get; set; }
    public bool? EnableBenfordLaw { get; set; }
    public bool? EnableRoundNumber { get; set; }
    public bool? EnableThresholdBreach { get; set; }
    public bool? EnableBackdating { get; set; }
    public bool? EnablePeriodEndCluster { get; set; }
    public bool? EnableFraudPatterns { get; set; }
    public bool? EnableNearDuplicates { get; set; }
    public bool? EnableIsolationForest { get; set; }
    public bool? EnableDbscan { get; set; }
    public bool? EnableAssociationRule { get; set; }
    public bool? EnableCopod { get; set; }
    public bool? EnableEcod { get; set; }
    public bool? EnableBehaviorProfiling { get; set; }
    public bool? EmailAlerts { get; set; }
    public string? AlertEmail { get; set; }
    public string? SlackWebhook { get; set; }
    public int? FiscalYearStartMonth { get; set; }
    public string? RelatedPartyList { get; set; } // JSON string
    public bool? ScheduledEnabled { get; set; }
    public string? ScheduledFrequency { get; set; }
    public int? ScheduledDayOfWeek { get; set; }
    public int? ScheduledHour { get; set; }
}

public class GlTestApiKeyRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? Model { get; set; }
}
