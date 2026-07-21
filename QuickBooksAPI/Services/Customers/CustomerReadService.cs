using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Mapping;

namespace QuickBooksAPI.Services.Customers;

public sealed class CustomerReadService : ICustomerReadService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerReadService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
    }

    public async Task<ApiResponse<IEnumerable<CustomerDto>>> ListAsync(int userId, string realmId)
    {
        var customers = await _customerRepository.GetAllByUserAndRealmAsync(userId, realmId);
        return ApiResponse<IEnumerable<CustomerDto>>.Ok(customers.Select(VendorCustomerReadMapping.ToDto));
    }

    public async Task<ApiResponse<PagedResult<CustomerDto>>> ListPagedAsync(int userId, string realmId, ListQueryParams query)
    {
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var activeFilter = query.GetActiveFilter();
        var result = await _customerRepository.GetPagedByUserAndRealmAsync(userId, realmId, page, pageSize, search, activeFilter, query.SortBy, query.IsDescending());
        return ApiResponse<PagedResult<CustomerDto>>.Ok(VendorCustomerReadMapping.ToCustomerDtoPaged(result));
    }

    public async Task<ApiResponse<CustomerDto>> GetByQboIdAsync(int userId, string realmId, string qboId)
    {
        if (string.IsNullOrWhiteSpace(qboId))
            return ApiResponse<CustomerDto>.Fail("Customer id is required.");

        var customer = await _customerRepository.GetByQboIdAsync(userId, realmId, qboId.Trim());
        if (customer == null)
            return ApiResponse<CustomerDto>.Fail("Customer not found.");

        return ApiResponse<CustomerDto>.Ok(VendorCustomerReadMapping.ToDto(customer));
    }
}
