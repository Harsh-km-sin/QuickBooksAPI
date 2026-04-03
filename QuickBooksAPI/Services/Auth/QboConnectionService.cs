using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;
using QuickBooksShared.Options;

namespace QuickBooksAPI.Services.Auth;

public class QboConnectionService : IQboConnectionService
{
    private readonly IQuickBooksAuthService _quickBooksAuthService;
    private readonly ITokenRepository _tokenRepo;
    private readonly IAppUserRepository _userRepo;
    private readonly ICompanyRepository _companyRepository;
    private readonly QuickBooksOptions _quickBooksOptions;
    private readonly ILogger<QboConnectionService> _logger;

    public QboConnectionService(
        IQuickBooksAuthService quickBooksAuthService,
        ITokenRepository tokenRepo,
        IAppUserRepository userRepo,
        ICompanyRepository companyRepository,
        IOptions<QuickBooksOptions> quickBooksOptions,
        ILogger<QboConnectionService> logger)
    {
        _quickBooksAuthService = quickBooksAuthService;
        _tokenRepo = tokenRepo;
        _userRepo = userRepo;
        _companyRepository = companyRepository;
        _quickBooksOptions = quickBooksOptions?.Value ?? throw new ArgumentNullException(nameof(quickBooksOptions));
        _logger = logger;
    }

    public async Task<string> GenerateOAuthUrlAsync(int userId)
    {
        var userExists = await _userRepo.UserExistsAsync(userId);
        if (!userExists)
            throw new ArgumentException("Invalid user ID.");

        var clientId = _quickBooksOptions.ClientId;
        var redirectUri = _quickBooksOptions.RedirectUri;
        var scope = _quickBooksOptions.Scopes;
        var url = _quickBooksOptions.AuthUrl;
        var state = $"{userId}_{Guid.NewGuid():N}";

        return $"{url}" +
               $"?client_id={clientId}" +
               $"&redirect_uri={redirectUri}" +
               $"&response_type=code" +
               $"&scope={scope}" +
               $"&state={state}";
    }

    public async Task<ApiResponse<QuickBooksToken>> HandleCallbackAsync(string code, string state, string realmId)
    {
        try
        {
            var stateParts = state?.Split('_');
            if (stateParts == null || stateParts.Length != 2)
                return ApiResponse<QuickBooksToken>.Fail("QuickBooks authentication failed.", new[] { "Invalid state parameter received from QuickBooks." });

            if (!int.TryParse(stateParts[0], out var userId))
                return ApiResponse<QuickBooksToken>.Fail("QuickBooks authentication failed.", new[] { "Invalid user ID in state parameter." });

            var userExists = await _userRepo.UserExistsAsync(userId);
            if (!userExists)
                return ApiResponse<QuickBooksToken>.Fail("QuickBooks authentication failed.", new[] { $"User with ID {userId} does not exist." });

            var existingToken = await _tokenRepo.GetTokenByUserAndRealmAsync(userId, realmId);
            if (existingToken != null)
                await _tokenRepo.DeleteTokenAsync(existingToken.Id);

            var tokenJson = await _quickBooksAuthService.HandleCallbackAsync(code, realmId);
            var tokenDto = JsonSerializer.Deserialize<TokenResponseDto>(tokenJson);

            if (tokenDto == null)
                return ApiResponse<QuickBooksToken>.Fail("QuickBooks authentication failed.", new[] { "Failed to retrieve authentication tokens from QuickBooks." });

            var token = new QuickBooksToken
            {
                UserId = userId,
                RealmId = realmId,
                IdToken = tokenDto.IdToken ?? string.Empty,
                AccessToken = tokenDto.AccessToken ?? string.Empty,
                RefreshToken = tokenDto.RefreshToken ?? string.Empty,
                TokenType = tokenDto.TokenType ?? "bearer",
                ExpiresIn = tokenDto.ExpiresIn,
                XRefreshTokenExpiresIn = tokenDto.XRefreshTokenExpiresIn,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _tokenRepo.SaveTokenAsync(token);

            string? companyName = null;
            try
            {
                var companyInfoJson = await _quickBooksAuthService.GetCompanyInfoAsync(token.AccessToken, realmId);
                var companyInfo = JsonSerializer.Deserialize<QuickBooksCompanyInfoResponse>(companyInfoJson);
                companyName = companyInfo?.CompanyInfo?.CompanyName ?? companyInfo?.CompanyInfo?.LegalName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch QuickBooks CompanyInfo for UserId={UserId}, RealmId={RealmId}", userId, realmId);
            }

            var company = new Company
            {
                UserId = userId,
                QboRealmId = realmId,
                CompanyName = companyName,
                QboAccessToken = token.AccessToken,
                QboRefreshToken = token.RefreshToken,
                TokenExpiryUtc = token.CreatedAt.AddSeconds(token.ExpiresIn),
                IsQboConnected = true,
                ConnectedAtUtc = token.CreatedAt,
                DisconnectedAtUtc = null
            };

            await _companyRepository.UpsertCompanyAsync(company);

            return ApiResponse<QuickBooksToken>.Ok(token, "QuickBooks token saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickBooks authentication failed during callback.");
            return ApiResponse<QuickBooksToken>.Fail("QuickBooks authentication failed. Please try again or reconnect your account.");
        }
    }

    public async Task<ApiResponse<string>> DisconnectQboAsync(int userId, string realmId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(realmId))
                return ApiResponse<string>.Fail("Realm ID is required to disconnect.");

            var token = await _tokenRepo.GetTokenByUserAndRealmAsync(userId, realmId);
            if (token == null)
                return ApiResponse<string>.Fail("No QuickBooks connection found for this company.");

            var revoked = await _quickBooksAuthService.DisconnectQboAsync(token.RefreshToken);
            if (!revoked)
            {
                _logger.LogWarning("Intuit revoke failed for UserId={UserId}, RealmId={RealmId}. Clearing local token anyway.", userId, realmId);
            }

            await _tokenRepo.DeleteTokenAsync(token.Id);
            await _companyRepository.ClearCompanyTokenAsync(userId, realmId);

            return ApiResponse<string>.Ok("QuickBooks company disconnected successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disconnect QBO failed. UserId={UserId}, RealmId={RealmId}", userId, realmId);
            return ApiResponse<string>.Fail("Failed to disconnect QuickBooks.", new[] { ex.Message });
        }
    }
}
