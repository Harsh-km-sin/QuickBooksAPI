using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IChartOfAccountsRepository
    {
        Task<int> UpsertChartOfAccountsAsync(IEnumerable<ChartOfAccounts> accounts);
        Task<IEnumerable<ChartOfAccounts>> GetAllByUserAndRealmAsync(int userId, string realmId);
    }
}
