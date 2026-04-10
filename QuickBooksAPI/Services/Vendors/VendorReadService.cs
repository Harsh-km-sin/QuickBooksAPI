using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Mapping;

namespace QuickBooksAPI.Services.Vendors;

public sealed class VendorReadService : IVendorReadService
{
    private readonly IVendorRepository _vendorRepository;

    public VendorReadService(IVendorRepository vendorRepository)
    {
        _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
    }

    public async Task<ApiResponse<IEnumerable<VendorDto>>> ListAsync(int userId, string realmId)
    {
        var vendors = await _vendorRepository.GetAllByUserAndRealmAsync(userId, realmId);
        return ApiResponse<IEnumerable<VendorDto>>.Ok(vendors.Select(VendorCustomerReadMapping.ToDto));
    }

    public async Task<ApiResponse<PagedResult<VendorDto>>> ListPagedAsync(int userId, string realmId, ListQueryParams query)
    {
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var activeFilter = query.GetActiveFilter();
        var result = await _vendorRepository.GetPagedByUserAndRealmAsync(userId, realmId, page, pageSize, search, activeFilter);
        return ApiResponse<PagedResult<VendorDto>>.Ok(VendorCustomerReadMapping.ToVendorDtoPaged(result));
    }
}
