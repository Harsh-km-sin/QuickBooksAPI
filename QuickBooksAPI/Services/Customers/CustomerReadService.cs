using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services.Customers;

public sealed class CustomerReadService : ICustomerReadService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerReadService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
    }

    public async Task<ApiResponse<IEnumerable<Customer>>> ListAsync(int userId, string realmId)
    {
        var customers = await _customerRepository.GetAllByUserAndRealmAsync(userId, realmId);
        return ApiResponse<IEnumerable<Customer>>.Ok(customers);
    }

    public async Task<ApiResponse<PagedResult<Customer>>> ListPagedAsync(int userId, string realmId, ListQueryParams query)
    {
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var activeFilter = query.GetActiveFilter();
        var result = await _customerRepository.GetPagedByUserAndRealmAsync(userId, realmId, page, pageSize, search, activeFilter);
        return ApiResponse<PagedResult<Customer>>.Ok(result);
    }

    public async Task<ApiResponse<Customer>> GetByQboIdAsync(int userId, string realmId, string qboId)
    {
        if (string.IsNullOrWhiteSpace(qboId))
            return ApiResponse<Customer>.Fail("Customer id is required.");

        var customer = await _customerRepository.GetByQboIdAsync(userId, realmId, qboId.Trim());
        if (customer == null)
            return ApiResponse<Customer>.Fail("Customer not found.");

        return ApiResponse<Customer>.Ok(customer);
    }
}
