using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public interface IUserRegistrationService
{
    Task<ApiResponse<int>> RegisterUserAsync(UserSignUpRequest request);
}
