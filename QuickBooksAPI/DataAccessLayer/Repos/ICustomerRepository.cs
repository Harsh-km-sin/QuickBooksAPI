using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface ICustomerRepository
    {
        Task<int> UpsertCustomersAsync(IEnumerable<Customer> customers, int userId, string realmId);
        Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId);
        Task<IEnumerable<Customer>> GetAllByUserAndRealmAsync(int userId, string realmId);
    }
}
