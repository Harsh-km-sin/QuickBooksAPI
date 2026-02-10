using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public class QuickBooksCustomerService : IQuickBooksCustomerService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public QuickBooksCustomerService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<string> GetCustomersAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();

            var query = "select * from Customer";
            
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
            query += " ORDERBY MetaData.LastUpdatedTime";
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
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}"
                );
            }
            
            return content;
        }
        public async Task<string> CreateCustomerAsync(string accessToken, string realmId, string customerPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            
            if (string.IsNullOrWhiteSpace(customerPayload))
                throw new ArgumentException("Customer payload cannot be null or empty.", nameof(customerPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{requestUrl}/{realmId}/customer"
            );
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(customerPayload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}"
                );
            }
            
            return content;
        }
        public async Task<string> UpdateCustomerAsync(string accessToken, string realmId, string customerPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            
            if (string.IsNullOrWhiteSpace(customerPayload))
                throw new ArgumentException("Customer payload cannot be null or empty.", nameof(customerPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{requestUrl}/{realmId}/customer"
            );
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(customerPayload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}"
                );
            }
            
            return content;
        }
        public async Task<string> DeleteCustomerAsync(string accessToken, string realmId, string customerPayload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            
            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));
            
            if (string.IsNullOrWhiteSpace(customerPayload))
                throw new ArgumentException("Customer payload cannot be null or empty.", nameof(customerPayload));

            var requestUrl = _config["QuickBooks:RequestURL"];
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{requestUrl}/{realmId}/customer"
            );
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(customerPayload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}"
                );
            }
            
            return content;
        }
    }
}
