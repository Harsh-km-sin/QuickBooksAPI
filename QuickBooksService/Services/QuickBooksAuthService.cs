using QuickBooksService.Services.AuthTransport;

namespace QuickBooksService.Services;

/// <summary>
/// Thin façade over QBO OAuth/company HTTP transports (<c>QuickBooksService.Services.AuthTransport</c>). Keeps <see cref="IQuickBooksAuthService"/> stable for app/integration code.
/// </summary>
public sealed class QuickBooksAuthService : IQuickBooksAuthService
{
    private readonly QboTokenExchangeClient _tokenExchange;
    private readonly QboTokenRefreshClient _tokenRefresh;
    private readonly QboTokenRevokeClient _tokenRevoke;
    private readonly QboCompanyInfoClient _companyInfo;

    public QuickBooksAuthService(
        QboTokenExchangeClient tokenExchange,
        QboTokenRefreshClient tokenRefresh,
        QboTokenRevokeClient tokenRevoke,
        QboCompanyInfoClient companyInfo)
    {
        _tokenExchange = tokenExchange ?? throw new ArgumentNullException(nameof(tokenExchange));
        _tokenRefresh = tokenRefresh ?? throw new ArgumentNullException(nameof(tokenRefresh));
        _tokenRevoke = tokenRevoke ?? throw new ArgumentNullException(nameof(tokenRevoke));
        _companyInfo = companyInfo ?? throw new ArgumentNullException(nameof(companyInfo));
    }

    public Task<string> HandleCallbackAsync(string code, string realmId) =>
        _tokenExchange.ExchangeAuthorizationCodeAsync(code, realmId);

    public Task<string> RefreshTokenAsync(string refreshToken) =>
        _tokenRefresh.RefreshAsync(refreshToken);

    public Task<bool> DisconnectQboAsync(string refreshToken) =>
        _tokenRevoke.RevokeRefreshTokenAsync(refreshToken);

    public Task<string> GetCompanyInfoAsync(string accessToken, string realmId) =>
        _companyInfo.GetCompanyInfoJsonAsync(accessToken, realmId);
}
