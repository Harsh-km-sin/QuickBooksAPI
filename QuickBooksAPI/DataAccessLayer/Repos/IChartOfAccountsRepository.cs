using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IChartOfAccountsRepository
    {
        public Task<int> UpsertChartOfAccountsAsync(IEnumerable<ChartOfAccounts> accounts);
    }
}
