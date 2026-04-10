using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface ITokenRepository
{
    Task SaveTokenAsync(QuickBooksToken token);
    Task<QuickBooksToken?> GetTokenByUserAndRealmAsync(int userId, string realmId);
    Task DeleteTokenAsync(int tokenId);
    Task<IEnumerable<string>> GetRealmIdsByUserIdAsync(int userId);
    Task UpdateTokenAsync(QuickBooksToken token);
}
