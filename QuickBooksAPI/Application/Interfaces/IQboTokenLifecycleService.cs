using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

/// <summary>QuickBooks Online access-token expiry checks and refresh (not application JWT).</summary>
public interface IQboTokenLifecycleService
{
    Task<bool> IsTokenExpiredAsync(QuickBooksToken? token);
    Task<QboAccessTokenSnapshot?> RefreshTokenIfExpiredAsync(int userId, string realmId);
}
