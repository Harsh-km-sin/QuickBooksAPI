namespace QuickBooksAPI.DataAccessLayer.Models;

public class GlSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // Risk tier thresholds (0-100 scale)
    public int ThresholdCritical { get; set; } = 65;
    public int ThresholdHigh { get; set; } = 40;
    public int ThresholdMedium { get; set; } = 20;
    public int ThresholdLow { get; set; } = 8;

    // Tier weights (worker auto-normalizes if sum != 1.0)
    public decimal WeightTier1Statistical { get; set; } = 0.25m;
    public decimal WeightTier2Ml { get; set; } = 0.35m;
    public decimal WeightTier3Rules { get; set; } = 0.25m;
    public decimal WeightTier4Llm { get; set; } = 0.15m;

    // LLM configuration
    public string LlmProvider { get; set; } = "anthropic"; // anthropic|openai|gemini|disabled
    public string LlmModel { get; set; } = "claude-sonnet-4-6";
    public string? ApiKeyAnthropic { get; set; }
    public string? ApiKeyOpenai { get; set; }
    public string? ApiKeyGoogle { get; set; }

    // Tier-1 detector toggles
    public bool EnableBenfordLaw { get; set; } = true;
    public bool EnableRoundNumber { get; set; } = true;
    public bool EnableThresholdBreach { get; set; } = true;
    public bool EnableBackdating { get; set; } = true;
    public bool EnablePeriodEndCluster { get; set; } = true;
    public bool EnableFraudPatterns { get; set; } = true;
    public bool EnableNearDuplicates { get; set; } = true;

    // Tier-2 ML detector toggles
    public bool EnableIsolationForest { get; set; } = true;
    public bool EnableDbscan { get; set; } = true;
    public bool EnableAssociationRule { get; set; } = true;
    public bool EnableCopod { get; set; } = true;
    public bool EnableEcod { get; set; } = true;
    public bool EnableBehaviorProfiling { get; set; } = true;

    // Notifications
    public bool EmailAlerts { get; set; }
    public string? AlertEmail { get; set; }
    public string? SlackWebhook { get; set; }

    // Fiscal year
    public int FiscalYearStartMonth { get; set; } = 1;

    // JSON: [{"name":"Acme Holdings","relationship":"parent_company"}]
    public string? RelatedPartyList { get; set; }

    // Scheduled auto-runs
    public bool ScheduledEnabled { get; set; }
    public string? ScheduledFrequency { get; set; } // daily|weekly|monthly
    public int? ScheduledDayOfWeek { get; set; }
    public int? ScheduledHour { get; set; }

    public DateTime UpdatedAt { get; set; }
}
