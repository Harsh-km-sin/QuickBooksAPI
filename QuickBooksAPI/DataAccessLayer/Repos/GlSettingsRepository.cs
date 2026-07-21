using Dapper;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.DataAccessLayer.Repos;

public class GlSettingsRepository : IGlSettingsRepository
{
    private readonly ISqlConnectionFactory _db;

    public GlSettingsRepository(ISqlConnectionFactory db) => _db = db;

    public async Task<GlSettings> GetOrCreateAsync(int userId, CancellationToken ct = default)
    {
        const string selectSql = "SELECT * FROM dbo.GL_Settings WHERE UserId = @UserId;";

        using var conn = _db.CreateConnection();
        var existing = await conn.QuerySingleOrDefaultAsync<GlSettings>(
            _db.CreateCommand(selectSql, new { UserId = userId }, ct));

        if (existing is not null)
            return existing;

        // Insert defaults and return
        var defaults = new GlSettings { UserId = userId, UpdatedAt = DateTime.UtcNow };
        const string insertSql = @"
INSERT INTO dbo.GL_Settings
    (UserId, ThresholdCritical, ThresholdHigh, ThresholdMedium, ThresholdLow,
     WeightTier1Statistical, WeightTier2Ml, WeightTier3Rules, WeightTier4Llm,
     LlmProvider, LlmModel,
     EnableBenfordLaw, EnableRoundNumber, EnableThresholdBreach, EnableBackdating,
     EnablePeriodEndCluster, EnableFraudPatterns, EnableNearDuplicates,
     EnableIsolationForest, EnableDbscan, EnableAssociationRule,
     EnableCopod, EnableEcod, EnableBehaviorProfiling,
     EmailAlerts, FiscalYearStartMonth, ScheduledEnabled, UpdatedAt)
VALUES
    (@UserId, @ThresholdCritical, @ThresholdHigh, @ThresholdMedium, @ThresholdLow,
     @WeightTier1Statistical, @WeightTier2Ml, @WeightTier3Rules, @WeightTier4Llm,
     @LlmProvider, @LlmModel,
     @EnableBenfordLaw, @EnableRoundNumber, @EnableThresholdBreach, @EnableBackdating,
     @EnablePeriodEndCluster, @EnableFraudPatterns, @EnableNearDuplicates,
     @EnableIsolationForest, @EnableDbscan, @EnableAssociationRule,
     @EnableCopod, @EnableEcod, @EnableBehaviorProfiling,
     @EmailAlerts, @FiscalYearStartMonth, @ScheduledEnabled, @UpdatedAt);
SELECT * FROM dbo.GL_Settings WHERE UserId = @UserId;";

        return await conn.QuerySingleAsync<GlSettings>(
            _db.CreateCommand(insertSql, defaults, ct));
    }

    public async Task UpdateAsync(GlSettings s, CancellationToken ct = default)
    {
        const string sql = @"
UPDATE dbo.GL_Settings SET
    ThresholdCritical        = @ThresholdCritical,
    ThresholdHigh            = @ThresholdHigh,
    ThresholdMedium          = @ThresholdMedium,
    ThresholdLow             = @ThresholdLow,
    WeightTier1Statistical   = @WeightTier1Statistical,
    WeightTier2Ml            = @WeightTier2Ml,
    WeightTier3Rules         = @WeightTier3Rules,
    WeightTier4Llm           = @WeightTier4Llm,
    LlmProvider              = @LlmProvider,
    LlmModel                 = @LlmModel,
    ApiKeyAnthropic          = @ApiKeyAnthropic,
    ApiKeyOpenai             = @ApiKeyOpenai,
    ApiKeyGoogle             = @ApiKeyGoogle,
    EnableBenfordLaw         = @EnableBenfordLaw,
    EnableRoundNumber        = @EnableRoundNumber,
    EnableThresholdBreach    = @EnableThresholdBreach,
    EnableBackdating         = @EnableBackdating,
    EnablePeriodEndCluster   = @EnablePeriodEndCluster,
    EnableFraudPatterns      = @EnableFraudPatterns,
    EnableNearDuplicates     = @EnableNearDuplicates,
    EnableIsolationForest    = @EnableIsolationForest,
    EnableDbscan             = @EnableDbscan,
    EnableAssociationRule    = @EnableAssociationRule,
    EnableCopod              = @EnableCopod,
    EnableEcod               = @EnableEcod,
    EnableBehaviorProfiling  = @EnableBehaviorProfiling,
    EmailAlerts              = @EmailAlerts,
    AlertEmail               = @AlertEmail,
    SlackWebhook             = @SlackWebhook,
    FiscalYearStartMonth     = @FiscalYearStartMonth,
    RelatedPartyList         = @RelatedPartyList,
    ScheduledEnabled         = @ScheduledEnabled,
    ScheduledFrequency       = @ScheduledFrequency,
    ScheduledDayOfWeek       = @ScheduledDayOfWeek,
    ScheduledHour            = @ScheduledHour,
    UpdatedAt                = @UpdatedAt
WHERE UserId = @UserId;";

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(_db.CreateCommand(sql, new
        {
            s.UserId,
            s.ThresholdCritical, s.ThresholdHigh, s.ThresholdMedium, s.ThresholdLow,
            s.WeightTier1Statistical, s.WeightTier2Ml, s.WeightTier3Rules, s.WeightTier4Llm,
            s.LlmProvider, s.LlmModel,
            s.ApiKeyAnthropic, s.ApiKeyOpenai, s.ApiKeyGoogle,
            s.EnableBenfordLaw, s.EnableRoundNumber, s.EnableThresholdBreach, s.EnableBackdating,
            s.EnablePeriodEndCluster, s.EnableFraudPatterns, s.EnableNearDuplicates,
            s.EnableIsolationForest, s.EnableDbscan, s.EnableAssociationRule,
            s.EnableCopod, s.EnableEcod, s.EnableBehaviorProfiling,
            s.EmailAlerts, s.AlertEmail, s.SlackWebhook,
            s.FiscalYearStartMonth, s.RelatedPartyList,
            s.ScheduledEnabled, s.ScheduledFrequency, s.ScheduledDayOfWeek, s.ScheduledHour,
            UpdatedAt = DateTime.UtcNow
        }, ct));
    }
}
