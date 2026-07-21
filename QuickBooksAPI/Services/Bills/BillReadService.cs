using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Mapping;

namespace QuickBooksAPI.Services.Bills;

public sealed class BillReadService : IBillReadService
{
    private readonly IBillRepository _billRepository;

    public BillReadService(IBillRepository billRepository)
    {
        _billRepository = billRepository ?? throw new ArgumentNullException(nameof(billRepository));
    }

    public async Task<ApiResponse<IEnumerable<BillListItemDto>>> ListAsync(string realmId)
    {
        var bills = await _billRepository.GetAllByRealmAsync(realmId);
        return ApiResponse<IEnumerable<BillListItemDto>>.Ok(bills.Select(InvoiceBillReadMapping.ToBillDto));
    }

    public async Task<ApiResponse<PagedResult<BillListItemDto>>> ListPagedAsync(string realmId, ListQueryParams query)
    {
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var result = await _billRepository.GetPagedByRealmAsync(realmId, page, pageSize, search, query.SortBy, query.IsDescending());
        return ApiResponse<PagedResult<BillListItemDto>>.Ok(InvoiceBillReadMapping.ToBillDtoPaged(result));
    }

    public async Task<ApiResponse<BillListItemDto>> GetByIdAsync(string realmId, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return ApiResponse<BillListItemDto>.Fail("Bill id is required.");

        var bill = await _billRepository.GetByQboBillIdAsync(realmId, id.Trim());
        if (bill == null)
            return ApiResponse<BillListItemDto>.Fail("Bill not found.");

        return ApiResponse<BillListItemDto>.Ok(InvoiceBillReadMapping.ToBillDto(bill));
    }
}
