using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IGlSettingsRepository
{
    /// <summary>Gets the user's settings row, creating defaults if none exists.</summary>
    Task<GlSettings> GetOrCreateAsync(int userId, CancellationToken ct = default);

    Task UpdateAsync(GlSettings settings, CancellationToken ct = default);
}
