using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces;

public interface IProductRepository
{
    Task<int> UpsertProductsAsync(IEnumerable<ProductUpsertDto> products);
    Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId);
    Task<IEnumerable<ProductDto>> GetAllByUserAndRealmAsync(int userId, string realmId);
    Task<PagedResult<ProductDto>> GetPagedByUserAndRealmAsync(int userId, string realmId, int page, int pageSize, string? search, bool? activeFilter = true);
}
