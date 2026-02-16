using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IProductRepository
    {
        Task<int> UpsertProductsAsync(IEnumerable<Products> products);
        Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId);
        Task<IEnumerable<Products>> GetAllByUserAndRealmAsync(int userId, string realmId);
        Task<PagedResult<Products>> GetPagedByUserAndRealmAsync(int userId, string realmId, int page, int pageSize, string? search);
    }
}
