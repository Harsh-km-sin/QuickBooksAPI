using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBooksShared.Options;

namespace QuickBooksService.Services.AuthTransport;

/// <summary>OAuth2 authorization_code → token exchange against Intuit token endpoint.</summary>
public sealed class QboTokenExchangeClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly QuickBooksOptions _options;
    private readonly ILogger<QboTokenExchangeClient> _logger;

    public QboTokenExchangeClient(
        IHttpClientFactory httpClientFactory,
        IOptions<QuickBooksOptions> quickBooksOptions,
        ILogger<QboTokenExchangeClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = quickBooksOptions?.Value ?? throw new ArgumentNullException(nameof(quickBooksOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ExchangeAuthorizationCodeAsync(string code, string realmId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));

        if (string.IsNullOrWhiteSpace(realmId))
            throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

        var clientId = _options.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("QuickBooks:ClientId configuration is missing or empty.");

        var clientSecret = _options.ClientSecret;
        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException("QuickBooks:ClientSecret configuration is missing or empty.");

        var redirectUri = _options.RedirectUri;
        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new InvalidOperationException("QuickBooks:RedirectUri configuration is missing or empty.");

        var tokenUrl = _options.TokenUrl;
        if (string.IsNullOrWhiteSpace(tokenUrl))
            throw new InvalidOperationException("QuickBooks:TokenUrl configuration is missing or empty.");

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectUri }
        });

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("QBO token exchange failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
            throw new HttpRequestException(
                $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}"
            );
        }

        _logger.LogDebug("QBO token exchange completed. RealmId={RealmId}", realmId);
        return content;
    }
}
