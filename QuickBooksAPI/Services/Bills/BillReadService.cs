using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services.Bills;

public sealed class BillReadService : IBillReadService
{
    private readonly IBillRepository _billRepository;

    public BillReadService(IBillRepository billRepository)
    {
        _billRepository = billRepository ?? throw new ArgumentNullException(nameof(billRepository));
    }

    public async Task<ApiResponse<IEnumerable<QBOBillHeader>>> ListAsync(string realmId)
    {
        var bills = await _billRepository.GetAllByRealmAsync(realmId);
        return ApiResponse<IEnumerable<QBOBillHeader>>.Ok(bills);
    }

    public async Task<ApiResponse<PagedResult<QBOBillHeader>>> ListPagedAsync(string realmId, ListQueryParams query)
    {
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var result = await _billRepository.GetPagedByRealmAsync(realmId, page, pageSize, search);
        return ApiResponse<PagedResult<QBOBillHeader>>.Ok(result);
    }

    public async Task<ApiResponse<QBOBillHeader>> GetByIdAsync(string realmId, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return ApiResponse<QBOBillHeader>.Fail("Bill id is required.");

        var bill = await _billRepository.GetByQboBillIdAsync(realmId, id.Trim());
        if (bill == null)
            return ApiResponse<QBOBillHeader>.Fail("Bill not found.");

        return ApiResponse<QBOBillHeader>.Ok(bill);
    }
}
