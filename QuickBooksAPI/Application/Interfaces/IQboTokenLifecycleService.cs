using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

/// <summary>QuickBooks Online access-token expiry checks and refresh (not application JWT).</summary>
public interface IQboTokenLifecycleService
{
    Task<bool> IsTokenExpiredAsync(QuickBooksToken? token);
    Task<QuickBooksToken?> RefreshTokenIfExpiredAsync(int userId, string realmId);
}
