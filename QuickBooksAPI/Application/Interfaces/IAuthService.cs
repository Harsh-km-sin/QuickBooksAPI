using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<int>> RegisterUserAsync(UserSignUpRequest request);
        Task<ApiResponse<string>> LoginUserAsync(UserLoginRequest request);
        Task<string> GenerateOAuthUrlAsync(int userId);
        Task<ApiResponse<QuickBooksToken>> HandleCallbackAsync(string code, string state, string realmId);
        Task<bool> IsTokenExpiredAsync(QuickBooksToken? token);
        Task<QuickBooksToken?> RefreshTokenIfExpiredAsync(int userId, string realmId);
        Task<ApiResponse<string>> DisconnectQboAsync(int userId, string realmId);
        Task<ApiResponse<IEnumerable<ConnectedCompanyDto>>> GetConnectedCompaniesAsync(int userId);
    }
}
