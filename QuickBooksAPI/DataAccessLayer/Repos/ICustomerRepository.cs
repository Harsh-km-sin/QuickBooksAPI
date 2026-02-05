using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface ICustomerRepository
    {
        //public Task<int> UpsertCustomersAsync(IEnumerable<Customer> customers);
        public Task<int> UpsertCustomersAsync(IEnumerable<Customer> customers, int userId, string realmId);

        public Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId);
    }
}
