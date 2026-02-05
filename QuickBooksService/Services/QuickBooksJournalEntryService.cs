using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QuickBooksService.Services
{
    public class QuickBooksJournalEntryService : IQuickBooksJournalEntryService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<QuickBooksJournalEntryService> _logger;

        public QuickBooksJournalEntryService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<QuickBooksJournalEntryService> logger)
        {
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetJournalEntryAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null)
        {
            var requestUrl = _config["QuickBooks:RequestURL"];
            var client = _httpClientFactory.CreateClient();

            var query = "select * from JournalEntry";

            // Add WHERE clause for MetaData.LastUpdatedTime if date filter is provided
            // QuickBooks API expects UTC timestamps in ISO 8601 format
            // Use ">" (not ">=") to skip already-synced records
            if (lastUpdatedAfter.HasValue)
            {
                // Ensure the DateTime is UTC (don't double-convert if already UTC)
                var utcDate = lastUpdatedAfter.Value.Kind == DateTimeKind.Utc 
                    ? lastUpdatedAfter.Value 
                    : lastUpdatedAfter.Value.ToUniversalTime();
                
                // Format as ISO 8601 UTC (yyyy-MM-ddTHH:mm:ssZ)
                var dateFilter = utcDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                query += $" WHERE MetaData.LastUpdatedTime > '{dateFilter}'";
            }

            query += $" startposition {startPosition} maxresults {maxResults}";

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{requestUrl}/{realmId}/query?query={Uri.EscapeDataString(query)}"
            );

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            request.Headers.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
            );

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("QBO JournalEntry request failed. StatusCode={StatusCode}, RealmId={RealmId}", response.StatusCode, realmId);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}."
                );
            }

            _logger.LogDebug("QBO JournalEntry query completed. RealmId={RealmId}, StartPosition={StartPosition}", realmId, startPosition);
            return content;
        }
    }
}
