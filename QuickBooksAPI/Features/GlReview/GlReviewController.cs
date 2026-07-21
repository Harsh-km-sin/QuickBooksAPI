using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Features.Shared;
using QuickBooksShared.Messages;
using System.Text;
using System.Text.Json;

namespace QuickBooksAPI.Features.GlReview;

[ApiController]
[Route("api/gl-review")]
[Authorize]
public class GlReviewController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".csv", ".xlsx", ".xls" };

    private const string GlContainer = "gl-uploads";
    private const int MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    private readonly IGlRunRepository _runs;
    private readonly IGlTransactionRepository _transactions;
    private readonly IGlFeedbackRepository _feedback;
    private readonly IGlChatRepository _chat;
    private readonly IGlSettingsRepository _settings;
    private readonly IGlAnomalyRepository _anomalies;
    private readonly IBlobStorageService _blob;
    private readonly IGlQueuePublisher _queue;
    private readonly IRequestContext _ctx;

    public GlReviewController(
        IGlRunRepository runs,
        IGlTransactionRepository transactions,
        IGlFeedbackRepository feedback,
        IGlChatRepository chat,
        IGlSettingsRepository settings,
        IGlAnomalyRepository anomalies,
        IBlobStorageService blob,
        IGlQueuePublisher queue,
        IRequestContext ctx)
    {
        _runs = runs;
        _transactions = transactions;
        _feedback = feedback;
        _chat = chat;
        _settings = settings;
        _anomalies = anomalies;
        _blob = blob;
        _queue = queue;
        _ctx = ctx;
    }

    // ── Upload ────────────────────────────────────────────────────────────────

    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file provided."));

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(ApiResponse<object>.Fail("File exceeds the 50 MB limit."));

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(ApiResponse<object>.Fail("Only .csv, .xlsx, and .xls files are accepted."));

        string blobPath;
        using (var stream = file.OpenReadStream())
            blobPath = await _blob.UploadAsync(stream, file.FileName, GlContainer, cancellationToken);

        var runId = await _runs.CreateAsync(new GlRun
        {
            UserId = userId,
            RealmId = _ctx.RealmId ?? string.Empty,
            FileName = file.FileName,
            FileType = ext.TrimStart('.').ToLowerInvariant(),
            FileSizeBytes = file.Length,
            BlobPath = blobPath,
            Status = "Pending",
            SourceFormat = "generic"
        }, cancellationToken);

        await _queue.PublishAsync(new GlAnalysisMessage
        {
            RunId = runId,
            UserId = userId,
            BlobPath = blobPath,
            FileName = file.FileName,
            RequestedAt = DateTime.UtcNow
        }, cancellationToken);

        return Ok(ApiResponse<GlUploadResponseDto>.Ok(new GlUploadResponseDto { RunId = runId, Status = "Pending" }));
    }

    // ── Runs ──────────────────────────────────────────────────────────────────

    [HttpGet("runs")]
    public async Task<IActionResult> ListRuns(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var runs = await _runs.ListByUserAsync(userId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GlRunDto>>.Ok(runs.Select(MapRun).ToList()));
    }

    [HttpGet("runs/{runId:int}")]
    public async Task<IActionResult> GetRun(int runId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        return Ok(ApiResponse<GlRunDto>.Ok(MapRun(run)));
    }

    [HttpPost("runs/{runId:int}/retry")]
    public async Task<IActionResult> RetryRun(int runId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        await _runs.RetryAsync(runId, userId, cancellationToken);

        await _queue.PublishAsync(new GlAnalysisMessage
        {
            RunId = runId,
            UserId = userId,
            BlobPath = run.BlobPath,
            FileName = run.FileName,
            RequestedAt = DateTime.UtcNow
        }, cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { runId, status = "Pending" }));
    }

    [HttpDelete("runs/{runId:int}")]
    public async Task<IActionResult> DeleteRun(int runId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        await _runs.DeleteAsync(runId, userId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    [HttpGet("runs/{runId:int}/summary")]
    public async Task<IActionResult> GetSummary(int runId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        var distribution = await _transactions.GetRiskDistributionAsync(runId, cancellationToken);
        var topFlagged = await _transactions.GetTopFlaggedAsync(runId, 10, cancellationToken);

        var summary = new GlRunSummaryDto
        {
            Run = MapRun(run),
            RiskDistribution = distribution,
            TopFlagged = topFlagged.Select(MapTransaction).ToList()
        };

        return Ok(ApiResponse<GlRunSummaryDto>.Ok(summary));
    }

    // ── Transactions ──────────────────────────────────────────────────────────

    [HttpGet("runs/{runId:int}/transactions")]
    public async Task<IActionResult> ListTransactions(
        int runId,
        [FromQuery] GlTransactionFilterRequest req,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        var filter = new GlTransactionFilter(
            Page: Math.Max(1, req.Page),
            PageSize: Math.Clamp(req.PageSize, 1, 200),
            RiskTier: req.RiskTier,
            AccountName: req.AccountName,
            EntityName: req.EntityName,
            DateFrom: req.DateFrom,
            DateTo: req.DateTo,
            AmountMin: req.AmountMin,
            AmountMax: req.AmountMax,
            UnreviewedOnly: req.UnreviewedOnly
        );

        var paged = await _transactions.ListPagedAsync(runId, filter, cancellationToken);
        var result = new PagedResult<GlTransactionDto>
        {
            Items = paged.Items.Select(MapTransaction).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };

        return Ok(ApiResponse<PagedResult<GlTransactionDto>>.Ok(result));
    }

    [HttpGet("runs/{runId:int}/transactions/{txId:int}")]
    public async Task<IActionResult> GetTransaction(int runId, int txId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        var tx = await _transactions.GetByIdAsync(txId, runId, cancellationToken);
        if (tx is null) return NotFound(ApiResponse<object>.Fail("Transaction not found."));

        var dto = MapTransaction(tx);

        // Enrich with per-detector anomaly details
        var anomalyDetails = await _anomalies.GetByEntryAsync(txId, cancellationToken);
        dto.AnomalyDetails = anomalyDetails.Select(a => new GlAnomalyDto
        {
            AnomalyType = a.AnomalyType,
            DetectorScore = a.DetectorScore,
            RiskReasons = DeserializeStringList(a.RiskReasons),
            Metadata = DeserializeObject(a.Metadata)
        }).ToList();

        // Enrich with feedback if exists
        var fb = await _feedback.GetByEntryAsync(txId, cancellationToken);
        if (fb is not null) dto.Feedback = MapFeedback(fb);

        return Ok(ApiResponse<GlTransactionDto>.Ok(dto));
    }

    [HttpPut("runs/{runId:int}/transactions/{txId:int}/review")]
    public async Task<IActionResult> MarkReviewed(int runId, int txId, [FromBody] GlMarkReviewedRequest req, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        await _transactions.MarkReviewedAsync(txId, runId, req.Note, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    // ── Feedback (upsert auditor verdict) ─────────────────────────────────────

    [HttpPut("runs/{runId:int}/transactions/{txId:int}/feedback")]
    public async Task<IActionResult> UpsertFeedback(
        int runId, int txId,
        [FromBody] GlFeedbackRequest req,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        var tx = await _transactions.GetByIdAsync(txId, runId, cancellationToken);
        if (tx is null) return NotFound(ApiResponse<object>.Fail("Transaction not found."));

        var fb = new GlFeedback
        {
            EntryId = txId,
            RunId = runId,
            UserId = userId,
            Status = req.Status,
            ResolutionStatus = req.ResolutionStatus,
            AuditDecision = req.AuditDecision,
            Comments = req.Comments,
            RequiredEvidence = req.RequiredEvidence,
            ResolutionNotes = req.ResolutionNotes,
            ReviewedBy = req.ReviewedBy,
            ReviewedAt = DateTime.UtcNow,
            AssignedTo = req.AssignedTo
        };

        await _feedback.UpsertAsync(fb, cancellationToken);
        return Ok(ApiResponse<GlFeedbackDto>.Ok(MapFeedback(fb)));
    }

    // ── Chat ──────────────────────────────────────────────────────────────────

    [HttpGet("runs/{runId:int}/chat")]
    public async Task<IActionResult> GetChatHistory(int runId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        var history = await _chat.GetHistoryAsync(runId, cancellationToken);
        var dtos = history
            .Where(m => m.Role != "system")
            .Select(m => new GlChatMessageDto { Id = m.Id, Role = m.Role, Content = m.Content, CreatedAt = m.CreatedAt })
            .ToList();

        return Ok(ApiResponse<IReadOnlyList<GlChatMessageDto>>.Ok(dtos));
    }

    [HttpPost("runs/{runId:int}/chat")]
    public async Task<IActionResult> SendChatMessage(
        int runId,
        [FromBody] GlChatRequest req,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        if (run.Status != "Complete")
            return BadRequest(ApiResponse<object>.Fail("Chat is only available for completed runs."));

        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(ApiResponse<object>.Fail("Message cannot be empty."));

        // Save user message
        await _chat.AddMessageAsync(new GlChatMessage
        {
            RunId = runId, UserId = userId, Role = "user", Content = req.Message
        }, cancellationToken);

        // Load settings for LLM provider/key
        var userSettings = await _settings.GetOrCreateAsync(userId, cancellationToken);
        var history = await _chat.GetHistoryAsync(runId, cancellationToken);

        // Call LLM
        string assistantReply;
        try
        {
            assistantReply = await CallChatLlmAsync(run, userSettings, history, req.Message, req.AiModel, cancellationToken);
        }
        catch (Exception ex)
        {
            assistantReply = $"I encountered an error processing your question. Please check your LLM configuration in GL Settings. ({ex.Message})";
        }

        // Save assistant reply
        await _chat.AddMessageAsync(new GlChatMessage
        {
            RunId = runId, UserId = userId, Role = "assistant", Content = assistantReply
        }, cancellationToken);

        return Ok(ApiResponse<GlChatMessageDto>.Ok(new GlChatMessageDto
        {
            Role = "assistant",
            Content = assistantReply,
            CreatedAt = DateTime.UtcNow
        }));
    }

    // ── Export ────────────────────────────────────────────────────────────────

    [HttpGet("runs/{runId:int}/export")]
    public async Task<IActionResult> ExportCsv(int runId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        var rows = await _transactions.GetAllForExportAsync(runId, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Id,TransactionDate,JournalEntryId,AccountName,AccountType,PostingType,Amount," +
                       "EntityName,Description,SourceType,CreatedBy,RiskScore,RiskTier," +
                       "CompositeRiskScore,ZScore,AnomalyFlags,AiExplanation,Status,IsReviewed,ReviewNote");

        foreach (var t in rows)
        {
            csv.AppendLine(string.Join(",",
                t.Id, t.TransactionDate, CsvEscape(t.JournalEntryId),
                CsvEscape(t.AccountName), CsvEscape(t.AccountType), CsvEscape(t.PostingType),
                t.Amount, CsvEscape(t.EntityName), CsvEscape(t.Description),
                CsvEscape(t.SourceType), CsvEscape(t.CreatedBy),
                t.RiskScore, CsvEscape(t.RiskTier),
                t.CompositeRiskScore?.ToString("F4"), t.ZScore?.ToString("F4"),
                CsvEscape(t.AnomalyFlags), CsvEscape(t.AiExplanation),
                CsvEscape(t.Status), t.IsReviewed ? "Yes" : "No", CsvEscape(t.ReviewNote)
            ));
        }

        var fileName = $"gl-analysis-run-{runId}-{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    // ── Account Stats (for charts) ────────────────────────────────────────────

    [HttpGet("runs/{runId:int}/account-stats")]
    public async Task<IActionResult> GetAccountStats(int runId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId, out var err))
            return BadRequest(ApiResponse<object>.Fail(err!));

        var run = await _runs.GetByIdAsync(runId, userId, cancellationToken);
        if (run is null) return NotFound(ApiResponse<object>.Fail("Run not found."));

        var stats = await _transactions.GetAccountStatsAsync(runId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DataAccessLayer.Models.GlAccountStats>>.Ok(stats));
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

    private static GlRunDto MapRun(GlRun r) => new()
    {
        Id = r.Id,
        FileName = r.FileName,
        FileType = r.FileType,
        FileSizeBytes = r.FileSizeBytes,
        Status = r.Status,
        ProgressPercentage = r.ProgressPercentage,
        SourceFormat = r.SourceFormat,
        TotalTransactions = r.TotalTransactions,
        FlaggedCount = r.FlaggedCount,
        CriticalCount = r.CriticalCount,
        HighCount = r.HighCount,
        MediumCount = r.MediumCount,
        LowCount = r.LowCount,
        NormalCount = r.NormalCount,
        AvgRiskScore = r.AvgRiskScore,
        MaterialExposure = r.MaterialExposure,
        PeriodStart = r.PeriodStart?.ToString("yyyy-MM-dd"),
        PeriodEnd = r.PeriodEnd?.ToString("yyyy-MM-dd"),
        AiExecutiveSummary = r.AiExecutiveSummary,
        CreatedAt = r.CreatedAt,
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt,
        ErrorMessage = r.ErrorMessage
    };

    private static GlTransactionDto MapTransaction(GlTransaction t) => new()
    {
        Id = t.Id,
        RunId = t.RunId,
        TransactionDate = t.TransactionDate.ToString("yyyy-MM-dd"),
        AccountId = t.AccountId,
        AccountName = t.AccountName,
        AccountType = t.AccountType,
        PostingType = t.PostingType,
        Amount = t.Amount,
        EntityName = t.EntityName,
        Description = t.Description,
        SourceType = t.SourceType,
        CreatedBy = t.CreatedBy,
        JournalEntryId = t.JournalEntryId,
        RiskScore = t.RiskScore,
        CompositeRiskScore = t.CompositeRiskScore,
        RiskTier = t.RiskTier,
        AnomalyFlags = DeserializeStringList(t.AnomalyFlags),
        ZScore = t.ZScore,
        AiExplanation = t.AiExplanation,
        RiskExplanation = DeserializeObject(t.RiskExplanation),
        Status = t.Status,
        IsReviewed = t.IsReviewed,
        ReviewedAt = t.ReviewedAt,
        ReviewNote = t.ReviewNote
    };

    private static GlFeedbackDto MapFeedback(GlFeedback f) => new()
    {
        Id = f.Id,
        EntryId = f.EntryId,
        Status = f.Status,
        ResolutionStatus = f.ResolutionStatus,
        AuditDecision = f.AuditDecision,
        Comments = f.Comments,
        RequiredEvidence = f.RequiredEvidence,
        ResolutionNotes = f.ResolutionNotes,
        ReviewedBy = f.ReviewedBy,
        ReviewedAt = f.ReviewedAt,
        AssignedTo = f.AssignedTo
    };

    private static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }

    private static object? DeserializeObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<object>(json); }
        catch { return null; }
    }

    private static string CsvEscape(string? value)
    {
        if (value is null) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static async Task<string> CallChatLlmAsync(
        GlRun run, GlSettings settings,
        IReadOnlyList<GlChatMessage> history,
        string userMessage, string? modelOverride,
        CancellationToken ct)
    {
        var systemPrompt = $"""
            You are a GL audit assistant. The user is reviewing a General Ledger analysis run.

            Run context:
            - File: {run.FileName}
            - Total entries: {run.TotalTransactions ?? 0}
            - Flagged: {run.FlaggedCount ?? 0}
            - Critical: {run.CriticalCount ?? 0}, High: {run.HighCount ?? 0}
            - Average risk score: {run.AvgRiskScore:F1}/100
            - Period: {run.PeriodStart} to {run.PeriodEnd}

            Answer questions about this GL run concisely and accurately.
            Do not fabricate specific transaction details you were not given.
            """;

        var model = modelOverride ?? settings.LlmModel;

        if (settings.LlmProvider == "anthropic" && !string.IsNullOrWhiteSpace(settings.ApiKeyAnthropic))
        {
            return await CallAnthropicAsync(settings.ApiKeyAnthropic, model, systemPrompt, history, userMessage, ct);
        }

        if (settings.LlmProvider == "openai" && !string.IsNullOrWhiteSpace(settings.ApiKeyOpenai))
        {
            return await CallOpenAiAsync(settings.ApiKeyOpenai, model, systemPrompt, history, userMessage, ct);
        }

        return "No LLM provider is configured. Please add an API key in GL Settings.";
    }

    private static async Task<string> CallAnthropicAsync(
        string apiKey, string model, string systemPrompt,
        IReadOnlyList<GlChatMessage> history, string userMessage, CancellationToken ct)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var messages = history
            .Where(m => m.Role is "user" or "assistant")
            .Select(m => new { role = m.Role, content = m.Content })
            .Append(new { role = "user", content = userMessage })
            .ToArray();

        var body = JsonSerializer.Serialize(new
        {
            model,
            max_tokens = 1024,
            system = systemPrompt,
            messages
        });

        var resp = await http.PostAsync(
            "https://api.anthropic.com/v1/messages",
            new StringContent(body, Encoding.UTF8, "application/json"), ct);

        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    private static async Task<string> CallOpenAiAsync(
        string apiKey, string model, string systemPrompt,
        IReadOnlyList<GlChatMessage> history, string userMessage, CancellationToken ct)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        messages.AddRange(history
            .Where(m => m.Role is "user" or "assistant")
            .Select(m => (object)new { role = m.Role, content = m.Content }));
        messages.Add(new { role = "user", content = userMessage });

        var body = JsonSerializer.Serialize(new { model, max_tokens = 1024, messages });
        var resp = await http.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(body, Encoding.UTF8, "application/json"), ct);

        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}
