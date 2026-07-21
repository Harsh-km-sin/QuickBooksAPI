using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using System.Text.Json;

namespace QuickBooksAPI.Features.GlReview;

[ApiController]
[Route("api/gl-review")]
[Authorize]
public class GlSettingsController : ControllerBase
{
    private readonly IGlSettingsRepository _settings;
    private readonly IGlBusinessRuleRepository _rules;
    private readonly IRequestContext _ctx;

    public GlSettingsController(
        IGlSettingsRepository settings,
        IGlBusinessRuleRepository rules,
        IRequestContext ctx)
    {
        _settings = settings;
        _rules = rules;
        _ctx = ctx;
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var s = await _settings.GetOrCreateAsync(userId, ct);
        return Ok(ApiResponse<GlSettingsDto>.Ok(MapSettings(s)));
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] GlSettingsRequest req, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var s = await _settings.GetOrCreateAsync(userId, ct);

        // Apply only provided (non-null) fields
        if (req.ThresholdCritical.HasValue) s.ThresholdCritical = req.ThresholdCritical.Value;
        if (req.ThresholdHigh.HasValue) s.ThresholdHigh = req.ThresholdHigh.Value;
        if (req.ThresholdMedium.HasValue) s.ThresholdMedium = req.ThresholdMedium.Value;
        if (req.ThresholdLow.HasValue) s.ThresholdLow = req.ThresholdLow.Value;
        if (req.WeightTier1Statistical.HasValue) s.WeightTier1Statistical = req.WeightTier1Statistical.Value;
        if (req.WeightTier2Ml.HasValue) s.WeightTier2Ml = req.WeightTier2Ml.Value;
        if (req.WeightTier3Rules.HasValue) s.WeightTier3Rules = req.WeightTier3Rules.Value;
        if (req.WeightTier4Llm.HasValue) s.WeightTier4Llm = req.WeightTier4Llm.Value;
        if (req.LlmProvider is not null) s.LlmProvider = req.LlmProvider;
        if (req.LlmModel is not null) s.LlmModel = req.LlmModel;
        if (req.ApiKeyAnthropic is not null) s.ApiKeyAnthropic = req.ApiKeyAnthropic;
        if (req.ApiKeyOpenai is not null) s.ApiKeyOpenai = req.ApiKeyOpenai;
        if (req.ApiKeyGoogle is not null) s.ApiKeyGoogle = req.ApiKeyGoogle;
        if (req.EnableBenfordLaw.HasValue) s.EnableBenfordLaw = req.EnableBenfordLaw.Value;
        if (req.EnableRoundNumber.HasValue) s.EnableRoundNumber = req.EnableRoundNumber.Value;
        if (req.EnableThresholdBreach.HasValue) s.EnableThresholdBreach = req.EnableThresholdBreach.Value;
        if (req.EnableBackdating.HasValue) s.EnableBackdating = req.EnableBackdating.Value;
        if (req.EnablePeriodEndCluster.HasValue) s.EnablePeriodEndCluster = req.EnablePeriodEndCluster.Value;
        if (req.EnableFraudPatterns.HasValue) s.EnableFraudPatterns = req.EnableFraudPatterns.Value;
        if (req.EnableNearDuplicates.HasValue) s.EnableNearDuplicates = req.EnableNearDuplicates.Value;
        if (req.EnableIsolationForest.HasValue) s.EnableIsolationForest = req.EnableIsolationForest.Value;
        if (req.EnableDbscan.HasValue) s.EnableDbscan = req.EnableDbscan.Value;
        if (req.EnableAssociationRule.HasValue) s.EnableAssociationRule = req.EnableAssociationRule.Value;
        if (req.EnableCopod.HasValue) s.EnableCopod = req.EnableCopod.Value;
        if (req.EnableEcod.HasValue) s.EnableEcod = req.EnableEcod.Value;
        if (req.EnableBehaviorProfiling.HasValue) s.EnableBehaviorProfiling = req.EnableBehaviorProfiling.Value;
        if (req.EmailAlerts.HasValue) s.EmailAlerts = req.EmailAlerts.Value;
        if (req.AlertEmail is not null) s.AlertEmail = req.AlertEmail;
        if (req.SlackWebhook is not null) s.SlackWebhook = req.SlackWebhook;
        if (req.FiscalYearStartMonth.HasValue) s.FiscalYearStartMonth = req.FiscalYearStartMonth.Value;
        if (req.RelatedPartyList is not null) s.RelatedPartyList = req.RelatedPartyList;
        if (req.ScheduledEnabled.HasValue) s.ScheduledEnabled = req.ScheduledEnabled.Value;
        if (req.ScheduledFrequency is not null) s.ScheduledFrequency = req.ScheduledFrequency;
        if (req.ScheduledDayOfWeek.HasValue) s.ScheduledDayOfWeek = req.ScheduledDayOfWeek.Value;
        if (req.ScheduledHour.HasValue) s.ScheduledHour = req.ScheduledHour.Value;

        await _settings.UpdateAsync(s, ct);
        return Ok(ApiResponse<GlSettingsDto>.Ok(MapSettings(s)));
    }

    [HttpPost("test-api-key")]
    public async Task<IActionResult> TestApiKey([FromBody] GlTestApiKeyRequest req, CancellationToken ct)
    {
        if (!TryGetUserId(out _, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        // Fire a minimal test prompt at the given provider
        try
        {
            var result = await CallTestPromptAsync(req.Provider, req.ApiKey, req.Model, ct);
            return Ok(ApiResponse<object>.Ok(new { success = result }));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object>.Ok(new { success = false, error = ex.Message }));
        }
    }

    // ── Business Rules ────────────────────────────────────────────────────────

    [HttpGet("business-rules")]
    public async Task<IActionResult> ListRules(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var rules = await _rules.ListByUserAsync(userId, ct);
        return Ok(ApiResponse<IReadOnlyList<GlBusinessRuleDto>>.Ok(rules.Select(MapRule).ToList()));
    }

    [HttpPost("business-rules")]
    public async Task<IActionResult> CreateRule([FromBody] GlBusinessRuleRequest req, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        if (string.IsNullOrWhiteSpace(req.RuleName))
            return BadRequest(ApiResponse<object>.Fail("RuleName is required."));

        var rule = new GlBusinessRule
        {
            UserId = userId,
            RuleName = req.RuleName,
            Description = req.Description,
            Condition = req.Condition.GetRawText(),
            Severity = req.Severity,
            IsActive = req.IsActive
        };

        var id = await _rules.CreateAsync(rule, ct);
        rule.Id = id;
        return Ok(ApiResponse<GlBusinessRuleDto>.Ok(MapRule(rule)));
    }

    [HttpPut("business-rules/{ruleId:int}")]
    public async Task<IActionResult> UpdateRule(int ruleId, [FromBody] GlBusinessRuleRequest req, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var existing = await _rules.GetByIdAsync(ruleId, userId, ct);
        if (existing is null) return NotFound(ApiResponse<object>.Fail("Rule not found."));

        existing.RuleName = req.RuleName;
        existing.Description = req.Description;
        existing.Condition = req.Condition.GetRawText();
        existing.Severity = req.Severity;
        existing.IsActive = req.IsActive;

        await _rules.UpdateAsync(existing, ct);
        return Ok(ApiResponse<GlBusinessRuleDto>.Ok(MapRule(existing)));
    }

    [HttpDelete("business-rules/{ruleId:int}")]
    public async Task<IActionResult> DeleteRule(int ruleId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        await _rules.DeleteAsync(ruleId, userId, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetUserId(out int userId, out string? error)
    {
        userId = 0;
        error = null;
        if (string.IsNullOrEmpty(_ctx.UserId) || !int.TryParse(_ctx.UserId, out userId))
        {
            error = "User context is missing. Please sign in.";
            return false;
        }
        return true;
    }

    private static GlSettingsDto MapSettings(GlSettings s) => new()
    {
        UserId = s.UserId,
        ThresholdCritical = s.ThresholdCritical,
        ThresholdHigh = s.ThresholdHigh,
        ThresholdMedium = s.ThresholdMedium,
        ThresholdLow = s.ThresholdLow,
        WeightTier1Statistical = s.WeightTier1Statistical,
        WeightTier2Ml = s.WeightTier2Ml,
        WeightTier3Rules = s.WeightTier3Rules,
        WeightTier4Llm = s.WeightTier4Llm,
        LlmProvider = s.LlmProvider,
        LlmModel = s.LlmModel,
        HasApiKeyAnthropic = !string.IsNullOrWhiteSpace(s.ApiKeyAnthropic),
        HasApiKeyOpenai = !string.IsNullOrWhiteSpace(s.ApiKeyOpenai),
        HasApiKeyGoogle = !string.IsNullOrWhiteSpace(s.ApiKeyGoogle),
        EnableBenfordLaw = s.EnableBenfordLaw,
        EnableRoundNumber = s.EnableRoundNumber,
        EnableThresholdBreach = s.EnableThresholdBreach,
        EnableBackdating = s.EnableBackdating,
        EnablePeriodEndCluster = s.EnablePeriodEndCluster,
        EnableFraudPatterns = s.EnableFraudPatterns,
        EnableNearDuplicates = s.EnableNearDuplicates,
        EnableIsolationForest = s.EnableIsolationForest,
        EnableDbscan = s.EnableDbscan,
        EnableAssociationRule = s.EnableAssociationRule,
        EnableCopod = s.EnableCopod,
        EnableEcod = s.EnableEcod,
        EnableBehaviorProfiling = s.EnableBehaviorProfiling,
        EmailAlerts = s.EmailAlerts,
        AlertEmail = s.AlertEmail,
        HasSlackWebhook = !string.IsNullOrWhiteSpace(s.SlackWebhook),
        FiscalYearStartMonth = s.FiscalYearStartMonth,
        RelatedPartyList = DeserializeRelatedParties(s.RelatedPartyList),
        ScheduledEnabled = s.ScheduledEnabled,
        ScheduledFrequency = s.ScheduledFrequency,
        ScheduledDayOfWeek = s.ScheduledDayOfWeek,
        ScheduledHour = s.ScheduledHour,
        UpdatedAt = s.UpdatedAt
    };

    private static List<RelatedPartyDto> DeserializeRelatedParties(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<List<RelatedPartyDto>>(json) ?? new(); }
        catch { return new(); }
    }

    private static GlBusinessRuleDto MapRule(GlBusinessRule r) => new()
    {
        Id = r.Id,
        RuleName = r.RuleName,
        Description = r.Description,
        Condition = JsonSerializer.Deserialize<object>(r.Condition) ?? new object(),
        Severity = r.Severity,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    private static async Task<bool> CallTestPromptAsync(string provider, string apiKey, string? model, CancellationToken ct)
    {
        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(15);

        if (provider == "anthropic")
        {
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            var body = JsonSerializer.Serialize(new
            {
                model = model ?? "claude-haiku-4-5-20251001",
                max_tokens = 10,
                messages = new[] { new { role = "user", content = "Reply OK" } }
            });
            var resp = await http.PostAsync(
                "https://api.anthropic.com/v1/messages",
                new StringContent(body, System.Text.Encoding.UTF8, "application/json"), ct);
            return resp.IsSuccessStatusCode;
        }

        if (provider == "openai")
        {
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            var body = JsonSerializer.Serialize(new
            {
                model = model ?? "gpt-4o-mini",
                max_tokens = 10,
                messages = new[] { new { role = "user", content = "Reply OK" } }
            });
            var resp = await http.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(body, System.Text.Encoding.UTF8, "application/json"), ct);
            return resp.IsSuccessStatusCode;
        }

        throw new NotSupportedException($"Provider '{provider}' is not supported for key testing.");
    }
}
