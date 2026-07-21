using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public record GlTransactionFilter(
    int Page,
    int PageSize,
    string? RiskTier = null,
    string? AccountName = null,
    string? EntityName = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    decimal? AmountMin = null,
    decimal? AmountMax = null,
    bool UnreviewedOnly = false
);

public interface IGlTransactionRepository
{
    Task<PagedResult<GlTransaction>> ListPagedAsync(int runId, GlTransactionFilter filter, CancellationToken cancellationToken = default);
    Task<GlTransaction?> GetByIdAsync(int id, int runId, CancellationToken cancellationToken = default);
    Task MarkReviewedAsync(int id, int runId, string? note, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlAccountStats>> GetAccountStatsAsync(int runId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetRiskDistributionAsync(int runId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlTransaction>> GetTopFlaggedAsync(int runId, int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlTransaction>> GetAllForExportAsync(int runId, CancellationToken cancellationToken = default);
}
