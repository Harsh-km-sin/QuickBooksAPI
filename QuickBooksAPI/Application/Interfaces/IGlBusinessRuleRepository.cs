using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IGlBusinessRuleRepository
{
    Task<IReadOnlyList<GlBusinessRule>> ListByUserAsync(int userId, CancellationToken ct = default);

    Task<int> CreateAsync(GlBusinessRule rule, CancellationToken ct = default);

    Task UpdateAsync(GlBusinessRule rule, CancellationToken ct = default);

    /// <summary>Delete only if the rule belongs to userId (ownership guard).</summary>
    Task DeleteAsync(int ruleId, int userId, CancellationToken ct = default);

    Task<GlBusinessRule?> GetByIdAsync(int ruleId, int userId, CancellationToken ct = default);
}
