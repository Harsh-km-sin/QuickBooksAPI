using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IProductService
    {
        Task<ApiResponse<int>> GetProductsAsync();
        Task<ApiResponse<string>> CreateProductAsync(CreateProductRequest request);
        Task<ApiResponse<string>> UpdateProductAsync(UpdateProductRequest request);
        Task<ApiResponse<string>> DeleteProductAsync(DeleteProductRequest request);
    }
}
