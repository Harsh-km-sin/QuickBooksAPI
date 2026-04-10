using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Mapping;
using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksService.Services;

namespace QuickBooksAPI.Services;

public partial class ProductServices : IProductService
{
    private readonly IRequestContext _requestContext;
    private readonly ITokenRepository _tokenRepository;
    private readonly IQuickBooksProductService _quickBooksProductService;
    private readonly IProductAccountingSyncGateway _productSyncGateway;
    private readonly IProductRepository _productRepository;
    private readonly IQboSyncStateRepository _qboSyncStateRepository;
    private readonly IAuthService _authService;

    public ProductServices(
        IRequestContext requestContext,
        ITokenRepository tokenRepository,
        IQuickBooksProductService quickBooksProductService,
        IProductAccountingSyncGateway productSyncGateway,
        IProductRepository productRepository,
        IQboSyncStateRepository qboSyncStateRepository,
        IAuthService authService)
    {
        _requestContext = requestContext;
        _tokenRepository = tokenRepository;
        _quickBooksProductService = quickBooksProductService;
        _productSyncGateway = productSyncGateway;
        _productRepository = productRepository;
        _qboSyncStateRepository = qboSyncStateRepository;
        _authService = authService;
    }

    public async Task<ApiResponse<IEnumerable<ProductDto>>> ListProductsAsync()
    {
        if (string.IsNullOrEmpty(_requestContext.UserId) || string.IsNullOrEmpty(_requestContext.RealmId))
            return ApiResponse<IEnumerable<ProductDto>>.Fail("User context is missing. Please sign in and connect QuickBooks.");

        var userId = int.Parse(_requestContext.UserId);
        var realmId = _requestContext.RealmId;
        var products = await _productRepository.GetAllByUserAndRealmAsync(userId, realmId);
        return ApiResponse<IEnumerable<ProductDto>>.Ok(products.Select(ProductReadMapping.ToDto));
    }

    public async Task<ApiResponse<PagedResult<ProductDto>>> ListProductsAsync(ListQueryParams query)
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
        return ApiResponse<PagedResult<ProductDto>>.Ok(ProductReadMapping.ToDtoPaged(result));
    }
}
