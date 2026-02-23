using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public class QuickBooksProductService : IQuickBooksProductService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<QuickBooksProductService> _logger;

        public QuickBooksProductService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<QuickBooksProductService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetProductsAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId)) throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl)) throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var query = "select * from Item";
            if (lastUpdatedAfter.HasValue)
            {
                var utcDate = lastUpdatedAfter.Value.Kind == DateTimeKind.Utc ? lastUpdatedAfter.Value : lastUpdatedAfter.Value.ToUniversalTime();
                query += $" WHERE MetaData.LastUpdatedTime > '{utcDate:yyyy-MM-ddTHH:mm:ssZ}'";
            }
            query += $" startposition {startPosition} maxresults {maxResults}";

            var request = new HttpRequestMessage(HttpMethod.Get, $"{requestUrl}/{realmId}/query?query={Uri.EscapeDataString(query)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO Products request failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException($"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            _logger.LogDebug("QBO Products query completed. RealmId={RealmId}, StartPosition={StartPosition}", realmId, startPosition);
            return content;
        }

        public async Task<string> CreateProductAsync(string accessToken, string realmId, string productPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId)) throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(productPayload)) throw new ArgumentException("Product payload cannot be null or empty.", nameof(productPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl)) throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{requestUrl}/{realmId}/item");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(productPayload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO CreateProduct failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException($"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            _logger.LogDebug("QBO CreateProduct completed. RealmId={RealmId}", realmId);
            return content;
        }

        public async Task<string> UpdateProductAsync(string accessToken, string realmId, string productPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId)) throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(productPayload)) throw new ArgumentException("Product payload cannot be null or empty.", nameof(productPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl)) throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{requestUrl}/{realmId}/item");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(productPayload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO UpdateProduct failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException($"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            _logger.LogDebug("QBO UpdateProduct completed. RealmId={RealmId}", realmId);
            return content;
        }

        public async Task<string> DeleteProductAsync(string accessToken, string realmId, string productPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId)) throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(productPayload)) throw new ArgumentException("Product payload cannot be null or empty.", nameof(productPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl)) throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{requestUrl}/{realmId}/item?operation=update");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(productPayload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO DeleteProduct failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException($"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            _logger.LogDebug("QBO DeleteProduct completed. RealmId={RealmId}", realmId);
            return content;
        }
    }
}
