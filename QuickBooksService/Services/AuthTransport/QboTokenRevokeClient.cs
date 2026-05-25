using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBooksShared.Options;

namespace QuickBooksService.Services.AuthTransport;

/// <summary>Intuit token revoke endpoint (disconnect).</summary>
public sealed class QboTokenRevokeClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly QuickBooksOptions _options;
    private readonly ILogger<QboTokenRevokeClient> _logger;

    public QboTokenRevokeClient(
        IHttpClientFactory httpClientFactory,
        IOptions<QuickBooksOptions> quickBooksOptions,
        ILogger<QboTokenRevokeClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = quickBooksOptions?.Value ?? throw new ArgumentNullException(nameof(quickBooksOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("RevokeRefreshTokenAsync called with null or empty refresh token.");
            return false;
        }

        var clientId = _options.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogError("QuickBooks:ClientId configuration is missing.");
            return false;
        }

        var clientSecret = _options.ClientSecret;
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogError("QuickBooks:ClientSecret configuration is missing.");
            return false;
        }

        var revokeUrl = _options.RevokeUrl;
        if (string.IsNullOrWhiteSpace(revokeUrl))
            throw new InvalidOperationException("QuickBooks:RevokeUrl configuration is missing or empty.");

        var client = _httpClientFactory.CreateClient();

        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        var body = JsonSerializer.Serialize(new { token = refreshToken });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(revokeUrl, content);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("QBO token revoked successfully.");
            return true;
        }

        _logger.LogWarning("QBO token revoke failed. StatusCode={StatusCode}", response.StatusCode);
        return false;
    }
}
