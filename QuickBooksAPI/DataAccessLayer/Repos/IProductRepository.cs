using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IProductRepository
    {
        Task<int> UpsertProductsAsync(IEnumerable<Products> products);
        Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId);
        Task<IEnumerable<Products>> GetAllByUserAndRealmAsync(int userId, string realmId);
    }
}
