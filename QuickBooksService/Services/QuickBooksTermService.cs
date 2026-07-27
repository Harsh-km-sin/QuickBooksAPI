using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBooksShared.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public class QuickBooksTermService : IQuickBooksTermService
    {
        private readonly QuickBooksOptions _quickBooksOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<QuickBooksTermService> _logger;

        public QuickBooksTermService(
            IHttpClientFactory httpClientFactory,
            IOptions<QuickBooksOptions> quickBooksOptions,
            ILogger<QuickBooksTermService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _quickBooksOptions = quickBooksOptions?.Value ?? throw new ArgumentNullException(nameof(quickBooksOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetTermsAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 1000)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

            var requestUrl = _quickBooksOptions.RequestURL;
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient();
            var query = $"select * from Term startposition {startPosition} maxresults {maxResults}";

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
                _logger.LogError("QBO Term query failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}", response.StatusCode, realmId, content);
                throw new HttpRequestException($"QBO request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}");
            }

            return content;
        }
    }
}
