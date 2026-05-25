using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Products.Handlers;

namespace QuickBooksAPI.Features.Products;

/// <summary>
/// MIGRATION-ONLY: Implements <see cref="IProductService"/> by delegating to feature handlers.
/// Prefer injecting handlers from this feature in new code; this type exists for <see cref="IProductService"/> compatibility (e.g. SyncWorker).
/// </summary>
public sealed class ProductServiceMigrationFacade : IProductService
{
    private readonly SyncProductsHandler _syncProducts;
    private readonly ListProductsHandler _listProducts;
    private readonly CreateProductHandler _createProduct;
    private readonly UpdateProductHandler _updateProduct;
    private readonly DeleteProductHandler _deleteProduct;

    public ProductServiceMigrationFacade(
        SyncProductsHandler syncProducts,
        ListProductsHandler listProducts,
        CreateProductHandler createProduct,
        UpdateProductHandler updateProduct,
        DeleteProductHandler deleteProduct)
    {
        _syncProducts = syncProducts;
        _listProducts = listProducts;
        _createProduct = createProduct;
        _updateProduct = updateProduct;
        _deleteProduct = deleteProduct;
    }

    public Task<ApiResponse<int>> GetProductsAsync() => _syncProducts.HandleAsync();

    public Task<ApiResponse<IEnumerable<ProductDto>>> ListProductsAsync() => _listProducts.HandleListAsync();

    public Task<ApiResponse<PagedResult<ProductDto>>> ListProductsAsync(ListQueryParams query) =>
        _listProducts.HandlePagedAsync(query);

    public Task<ApiResponse<string>> CreateProductAsync(CreateProductRequest request) =>
        _createProduct.HandleAsync(request);

    public Task<ApiResponse<string>> UpdateProductAsync(UpdateProductRequest request) =>
        _updateProduct.HandleAsync(request);

    public Task<ApiResponse<string>> DeleteProductAsync(DeleteProductRequest request) =>
        _deleteProduct.HandleAsync(request);
}
