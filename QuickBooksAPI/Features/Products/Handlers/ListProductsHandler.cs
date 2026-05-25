using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Features.Products.Handlers;

public sealed class ListProductsHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IProductRepository _productRepository;

    public ListProductsHandler(IRequestContext requestContext, IProductRepository productRepository)
    {
        _requestContext = requestContext;
        _productRepository = productRepository;
    }

    public async Task<ApiResponse<IEnumerable<ProductDto>>> HandleListAsync()
    {
        if (string.IsNullOrEmpty(_requestContext.UserId) || string.IsNullOrEmpty(_requestContext.RealmId))
            return ApiResponse<IEnumerable<ProductDto>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

        var userId = int.Parse(_requestContext.UserId);
        var realmId = _requestContext.RealmId;
        var rows = await _productRepository.GetAllByUserAndRealmAsync(userId, realmId);
        return ApiResponse<IEnumerable<ProductDto>>.Ok(rows);
    }

    public async Task<ApiResponse<PagedResult<ProductDto>>> HandlePagedAsync(ListQueryParams query)
    {
        if (string.IsNullOrEmpty(_requestContext.UserId) || string.IsNullOrEmpty(_requestContext.RealmId))
            return ApiResponse<PagedResult<ProductDto>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

        var userId = int.Parse(_requestContext.UserId);
        var realmId = _requestContext.RealmId;
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var activeFilter = query.GetActiveFilter();
        var result = await _productRepository.GetPagedByUserAndRealmAsync(userId, realmId, page, pageSize, search, activeFilter);
        return ApiResponse<PagedResult<ProductDto>>.Ok(result);
    }
}
