using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface ICompanyRepository
    {
        Task<Company?> GetByUserAndRealmAsync(int userId, string realmId);
        Task<IEnumerable<Company>> GetConnectedCompaniesByUserIdAsync(int userId);
        Task UpsertCompanyAsync(Company company);
        Task ClearCompanyTokenAsync(int userId, string realmId);
    }
}

