using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IProductService
    {
        Task<ApiResponse<int>> GetProductsAsync();
        Task<ApiResponse<IEnumerable<ProductDto>>> ListProductsAsync();
        Task<ApiResponse<PagedResult<ProductDto>>> ListProductsAsync(ListQueryParams query);
        Task<ApiResponse<string>> CreateProductAsync(CreateProductRequest request);
        Task<ApiResponse<string>> UpdateProductAsync(UpdateProductRequest request);
        Task<ApiResponse<string>> DeleteProductAsync(DeleteProductRequest request);
    }
}
