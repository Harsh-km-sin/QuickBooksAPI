using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public class QuickBooksBillService : IQuickBooksBillService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<QuickBooksBillService> _logger;

        public QuickBooksBillService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<QuickBooksBillService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetBillsAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();

            var query = "select * from Bill";

            if (lastUpdatedAfter.HasValue)
            {
                var utcDate = lastUpdatedAfter.Value.Kind == DateTimeKind.Utc
                    ? lastUpdatedAfter.Value
                    : lastUpdatedAfter.Value.ToUniversalTime();
                var dateFilter = utcDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                query += $" WHERE MetaData.LastUpdatedTime > '{dateFilter}'";
            }
            query += " ORDERBY MetaData.LastUpdatedTime ASC";
            query += $" startposition {startPosition} maxresults {maxResults}";

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{requestUrl}/{realmId}/query?query={Uri.EscapeDataString(query)}"
            );

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO Bill request failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }

            _logger.LogDebug("QBO Bill query completed. RealmId={RealmId}, StartPosition={StartPosition}", realmId, startPosition);
            return content;
        }

        public async Task<string> CreateBillAsync(string accessToken, string realmId, string billPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(billPayload))
                throw new ArgumentException("Bill payload cannot be null or empty.", nameof(billPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{requestUrl}/{realmId}/bill")
            {
                Content = new StringContent(billPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO Bill create failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            return content;
        }

        public async Task<string> UpdateBillAsync(string accessToken, string realmId, string billPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(billPayload))
                throw new ArgumentException("Bill payload cannot be null or empty.", nameof(billPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{requestUrl}/{realmId}/bill")
            {
                Content = new StringContent(billPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO Bill update failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            return content;
        }

        public async Task<string> DeleteBillAsync(string accessToken, string realmId, string billPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(billPayload))
                throw new ArgumentException("Bill payload cannot be null or empty.", nameof(billPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{requestUrl}/{realmId}/bill?operation=delete")
            {
                Content = new StringContent(billPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO Bill delete failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            return content;
        }
    }
}
