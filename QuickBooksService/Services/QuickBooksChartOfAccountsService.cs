using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public class QuickBooksChartOfAccountsService : IQuickBooksChartOfAccountsService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<QuickBooksChartOfAccountsService> _logger;

        public QuickBooksChartOfAccountsService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<QuickBooksChartOfAccountsService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetChartOfAccountsAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var query = "select * from Account";
            if (lastUpdatedAfter.HasValue)
            {
                var utcDate = lastUpdatedAfter.Value.Kind == DateTimeKind.Utc ? lastUpdatedAfter.Value : lastUpdatedAfter.Value.ToUniversalTime();
                var dateFilter = utcDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                query += $" WHERE MetaData.LastUpdatedTime > '{dateFilter}'";
            }
            query += $" startposition {startPosition} maxresults {maxResults}";

            var request = new HttpRequestMessage(HttpMethod.Get, $"{requestUrl}/{realmId}/query?query={Uri.EscapeDataString(query)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("QBO ChartOfAccounts request failed. StatusCode={StatusCode}, RealmId={RealmId}", response.StatusCode, realmId);
                throw new HttpRequestException($"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}.");
            }
            _logger.LogDebug("QBO ChartOfAccounts query completed. RealmId={RealmId}, StartPosition={StartPosition}", realmId, startPosition);
            return content;
        }
    }
}
