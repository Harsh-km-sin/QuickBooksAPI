using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public class QuickBooksInvoiceService : IQuickBooksInvoiceService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<QuickBooksInvoiceService> _logger;

        public QuickBooksInvoiceService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<QuickBooksInvoiceService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetInvoiceAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();

            var query = "select * from Invoice";
            
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
                _logger.LogError("QBO Invoice request failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}"
                );
            }

            _logger.LogDebug("QBO Invoice query completed. RealmId={RealmId}, StartPosition={StartPosition}", realmId, startPosition);
            return content;
        }

        public async Task<string> CreateInvoiceAsync(string accessToken, string realmId, string invoicePayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(invoicePayload))
                throw new ArgumentException("Invoice payload cannot be null or empty.", nameof(invoicePayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{requestUrl}/{realmId}/invoice")
            {
                Content = new StringContent(invoicePayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO Invoice create failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            return content;
        }

        public async Task<string> UpdateInvoiceAsync(string accessToken, string realmId, string invoicePayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(invoicePayload))
                throw new ArgumentException("Invoice payload cannot be null or empty.", nameof(invoicePayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{requestUrl}/{realmId}/invoice")
            {
                Content = new StringContent(invoicePayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO Invoice update failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            return content;
        }

        public async Task<string> DeleteInvoiceAsync(string accessToken, string realmId, string invoicePayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(invoicePayload))
                throw new ArgumentException("Invoice payload cannot be null or empty.", nameof(invoicePayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{requestUrl}/{realmId}/invoice?operation=delete")
            {
                Content = new StringContent(invoicePayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO Invoice delete failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            return content;
        }

        public async Task<string> VoidInvoiceAsync(string accessToken, string realmId, string invoicePayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            if (string.IsNullOrWhiteSpace(invoicePayload))
                throw new ArgumentException("Invoice payload cannot be null or empty.", nameof(invoicePayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{requestUrl}/{realmId}/invoice?operation=void")
            {
                Content = new StringContent(invoicePayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QBO Invoice void failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }
            return content;
        }
    }
}
