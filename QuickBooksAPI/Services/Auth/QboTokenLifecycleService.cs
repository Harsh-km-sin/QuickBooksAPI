using System.Text.Json;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksService.Services;

namespace QuickBooksAPI.Services.Auth;

public class QboTokenLifecycleService : IQboTokenLifecycleService
{
    private readonly IQuickBooksAuthService _quickBooksAuthService;
    private readonly ITokenRepository _tokenRepo;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<QboTokenLifecycleService> _logger;

    public QboTokenLifecycleService(
        IQuickBooksAuthService quickBooksAuthService,
        ITokenRepository tokenRepo,
        ICompanyRepository companyRepository,
        ILogger<QboTokenLifecycleService> logger)
    {
        _quickBooksAuthService = quickBooksAuthService;
        _tokenRepo = tokenRepo;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public Task<bool> IsTokenExpiredAsync(QuickBooksToken? token)
    {
        if (token == null)
            return Task.FromResult(true);

        var expirationTime = token.CreatedAt.AddSeconds(token.ExpiresIn);
        var bufferTime = expirationTime.AddMinutes(-1);
        return Task.FromResult(DateTime.UtcNow >= bufferTime);
    }

    public async Task<QuickBooksToken?> RefreshTokenIfExpiredAsync(int userId, string realmId)
    {
        try
        {
            var token = await _tokenRepo.GetTokenByUserAndRealmAsync(userId, realmId);
            if (token == null)
                return null;

            if (!await IsTokenExpiredAsync(token))
                return token;

            var refreshResponseJson = await _quickBooksAuthService.RefreshTokenAsync(token.RefreshToken);
            var refreshResponse = JsonSerializer.Deserialize<TokenResponseDto>(refreshResponseJson);

            if (refreshResponse == null)
                return null;

            token.IdToken = refreshResponse.IdToken ?? token.IdToken;
            token.AccessToken = refreshResponse.AccessToken ?? token.AccessToken;
            token.RefreshToken = refreshResponse.RefreshToken ?? token.RefreshToken;
            token.TokenType = refreshResponse.TokenType ?? token.TokenType;
            token.ExpiresIn = refreshResponse.ExpiresIn;
            token.XRefreshTokenExpiresIn = refreshResponse.XRefreshTokenExpiresIn;
            token.CreatedAt = DateTime.UtcNow;
            token.UpdatedAt = DateTime.UtcNow;

            await _tokenRepo.UpdateTokenAsync(token);

            var company = new Company
            {
                UserId = userId,
                QboRealmId = realmId,
                CompanyName = null,
                QboAccessToken = token.AccessToken,
                QboRefreshToken = token.RefreshToken,
                TokenExpiryUtc = token.CreatedAt.AddSeconds(token.ExpiresIn),
                IsQboConnected = true,
                ConnectedAtUtc = token.CreatedAt,
                DisconnectedAtUtc = null
            };

            await _companyRepository.UpsertCompanyAsync(company);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh failed. UserId={UserId}, RealmId={RealmId}", userId, realmId);
            return null;
        }
    }
}
