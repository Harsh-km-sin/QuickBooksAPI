using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface IConnectedCompanyQueryService
{
    Task<ApiResponse<IEnumerable<ConnectedCompanyDto>>> GetConnectedCompaniesAsync(int userId);
}
