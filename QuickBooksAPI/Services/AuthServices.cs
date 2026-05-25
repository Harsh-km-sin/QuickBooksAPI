using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Services.Auth;

namespace QuickBooksAPI.Services;

/// <summary>Compatibility façade over focused auth/QBO services (Phase 3 decomposition).</summary>
public class AuthServices : IAuthService
{
    private readonly IUserRegistrationService _registration;
    private readonly IUserLoginService _login;
    private readonly IQboConnectionService _qboConnection;
    private readonly IQboTokenLifecycleService _qboTokenLifecycle;
    private readonly IConnectedCompanyQueryService _connectedCompanies;

    public AuthServices(
        IUserRegistrationService registration,
        IUserLoginService login,
        IQboConnectionService qboConnection,
        IQboTokenLifecycleService qboTokenLifecycle,
        IConnectedCompanyQueryService connectedCompanies)
    {
        _registration = registration;
        _login = login;
        _qboConnection = qboConnection;
        _qboTokenLifecycle = qboTokenLifecycle;
        _connectedCompanies = connectedCompanies;
    }

    public Task<ApiResponse<int>> RegisterUserAsync(UserSignUpRequest request) =>
        _registration.RegisterUserAsync(request);

    public Task<ApiResponse<string>> LoginUserAsync(UserLoginRequest request) =>
        _login.LoginUserAsync(request);

    public Task<string> GenerateOAuthUrlAsync(int userId) =>
        _qboConnection.GenerateOAuthUrlAsync(userId);

    public Task<ApiResponse<QuickBooksToken>> HandleCallbackAsync(string code, string state, string realmId) =>
        _qboConnection.HandleCallbackAsync(code, state, realmId);

    public Task<bool> IsTokenExpiredAsync(QuickBooksToken? token) =>
        _qboTokenLifecycle.IsTokenExpiredAsync(token);

    public Task<QboAccessTokenSnapshot?> RefreshTokenIfExpiredAsync(int userId, string realmId) =>
        _qboTokenLifecycle.RefreshTokenIfExpiredAsync(userId, realmId);

    public Task<ApiResponse<string>> DisconnectQboAsync(int userId, string realmId) =>
        _qboConnection.DisconnectQboAsync(userId, realmId);

    public Task<ApiResponse<IEnumerable<ConnectedCompanyDto>>> GetConnectedCompaniesAsync(int userId) =>
        _connectedCompanies.GetConnectedCompaniesAsync(userId);
}
