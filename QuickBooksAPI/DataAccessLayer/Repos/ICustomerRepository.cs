using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface ICustomerRepository
    {
        Task<int> UpsertCustomersAsync(IEnumerable<Customer> customers, int userId, string realmId);
        Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId);
        Task<IEnumerable<Customer>> GetAllByUserAndRealmAsync(int userId, string realmId);
        Task<PagedResult<Customer>> GetPagedByUserAndRealmAsync(int userId, string realmId, int page, int pageSize, string? search, bool? activeFilter = true);
        Task<Customer?> GetByQboIdAsync(int userId, string realmId, string qboId);
    }
}
