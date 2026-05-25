using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBooksShared.Options;

namespace QuickBooksService.Services.AuthTransport;

/// <summary>OAuth2 refresh_token grant against Intuit token endpoint.</summary>
public sealed class QboTokenRefreshClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly QuickBooksOptions _options;
    private readonly ILogger<QboTokenRefreshClient> _logger;

    public QboTokenRefreshClient(
        IHttpClientFactory httpClientFactory,
        IOptions<QuickBooksOptions> quickBooksOptions,
        ILogger<QboTokenRefreshClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = quickBooksOptions?.Value ?? throw new ArgumentNullException(nameof(quickBooksOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token cannot be null or empty.", nameof(refreshToken));

        var clientId = _options.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("QuickBooks:ClientId configuration is missing or empty.");

        var clientSecret = _options.ClientSecret;
        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException("QuickBooks:ClientSecret configuration is missing or empty.");

        var tokenUrl = _options.TokenUrl;
        if (string.IsNullOrWhiteSpace(tokenUrl))
            throw new InvalidOperationException("QuickBooks:TokenUrl configuration is missing or empty.");

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        });

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("QBO token refresh failed. StatusCode={StatusCode}, Response={ResponseBody}", response.StatusCode, content);
            throw new HttpRequestException(
                $"QBO token refresh failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}"
            );
        }

        _logger.LogDebug("QBO token refresh completed.");
        return content;
    }
}
