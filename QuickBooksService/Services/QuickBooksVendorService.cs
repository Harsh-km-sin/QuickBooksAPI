using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public class QuickBooksVendorService : IQuickBooksVendorService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<QuickBooksVendorService> _logger;

        public QuickBooksVendorService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<QuickBooksVendorService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetVendorsAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var query = "select * from Vendor";
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
                _logger.LogWarning("QBO Vendors request failed. StatusCode={StatusCode}, RealmId={RealmId}", response.StatusCode, realmId);
                throw new HttpRequestException($"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}.");
            }
            _logger.LogDebug("QBO Vendors query completed. RealmId={RealmId}, StartPosition={StartPosition}", realmId, startPosition);
            return content;
        }

        public async Task<string> CreateVendorAsync(string accessToken, string realmId, string vendorPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId)) throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(vendorPayload)) throw new ArgumentException("Vendor payload cannot be null or empty.", nameof(vendorPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl)) throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{requestUrl}/{realmId}/vendor");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(vendorPayload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("QBO CreateVendor failed. StatusCode={StatusCode}, RealmId={RealmId}", response.StatusCode, realmId);
                throw new HttpRequestException($"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}.");
            }
            _logger.LogDebug("QBO CreateVendor completed. RealmId={RealmId}", realmId);
            return content;
        }

        public async Task<string> UpdateVendorAsync(string accessToken, string realmId, string vendorPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId)) throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(vendorPayload)) throw new ArgumentException("Vendor payload cannot be null or empty.", nameof(vendorPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl)) throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{requestUrl}/{realmId}/vendor");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(vendorPayload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("QBO UpdateVendor failed. StatusCode={StatusCode}, RealmId={RealmId}", response.StatusCode, realmId);
                throw new HttpRequestException($"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}.");
            }
            _logger.LogDebug("QBO UpdateVendor completed. RealmId={RealmId}", realmId);
            return content;
        }

        /// <summary>Soft-deletes a vendor in QuickBooks (sets Active = false). Uses a dedicated request; does not call UpdateVendorAsync.</summary>
        public async Task<string> SoftDeleteVendorAsync(string accessToken, string realmId, string id, string syncToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId)) throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Vendor Id cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(syncToken)) throw new ArgumentException("SyncToken cannot be null or empty.", nameof(syncToken));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl)) throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var payload = new { Id = id, SyncToken = syncToken, sparse = true, Active = false };
            var body = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = null });

            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{requestUrl}/{realmId}/vendor?operation=update"
            );

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("QBO SoftDeleteVendor failed. StatusCode={StatusCode}, RealmId={RealmId}, VendorId={VendorId}", response.StatusCode, realmId, id);
                throw new HttpRequestException($"QBO soft-delete vendor failed. Status={(int)response.StatusCode} {response.ReasonPhrase}.");
            }
            _logger.LogDebug("QBO SoftDeleteVendor completed. RealmId={RealmId}, VendorId={VendorId}", realmId, id);
            return content;
        }
    }
}
