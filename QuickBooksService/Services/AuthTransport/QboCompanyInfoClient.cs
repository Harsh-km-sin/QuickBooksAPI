using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBooksShared.Options;

namespace QuickBooksService.Services.AuthTransport;

/// <summary>QuickBooks Online company info JSON (minor version 65-style path).</summary>
public sealed class QboCompanyInfoClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly QuickBooksOptions _options;
    private readonly ILogger<QboCompanyInfoClient> _logger;

    public QboCompanyInfoClient(
        IHttpClientFactory httpClientFactory,
        IOptions<QuickBooksOptions> quickBooksOptions,
        ILogger<QboCompanyInfoClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = quickBooksOptions?.Value ?? throw new ArgumentNullException(nameof(quickBooksOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetCompanyInfoJsonAsync(string accessToken, string realmId)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

        if (string.IsNullOrWhiteSpace(realmId))
            throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

        var baseUrl = _options.RequestURL;
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

        var requestUrl = $"{baseUrl.TrimEnd('/')}/{realmId}/companyinfo/{realmId}";

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("QBO CompanyInfo request failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
            throw new HttpRequestException(
                $"QBO CompanyInfo request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
        }

        _logger.LogDebug("QBO CompanyInfo request succeeded. RealmId={RealmId}", realmId);
        return content;
    }
}
