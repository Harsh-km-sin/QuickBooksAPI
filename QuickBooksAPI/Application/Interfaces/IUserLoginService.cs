using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface IUserLoginService
{
    Task<ApiResponse<string>> LoginUserAsync(UserLoginRequest request);
}
