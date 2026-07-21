using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBooksShared.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public class QuickBooksReportsService : IQuickBooksReportsService
    {
        /// <summary>Named HttpClient this service uses — the only client the QBO retry/backoff handler is attached to.</summary>
        public const string HttpClientName = "QboReports";

        private readonly QuickBooksOptions _quickBooksOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<QuickBooksReportsService> _logger;

        public QuickBooksReportsService(
            IHttpClientFactory httpClientFactory,
            IOptions<QuickBooksOptions> quickBooksOptions,
            ILogger<QuickBooksReportsService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _quickBooksOptions = quickBooksOptions?.Value ?? throw new ArgumentNullException(nameof(quickBooksOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<string> GetProfitAndLossAsync(
            string accessToken,
            string realmId,
            DateTime startDate,
            DateTime endDate,
            string summarizeColumnBy = "Month",
            string accountingMethod = "Accrual") =>
            GetReportAsync("ProfitAndLoss", accessToken, realmId, startDate, endDate, summarizeColumnBy, accountingMethod);

        public Task<string> GetBalanceSheetAsync(
            string accessToken,
            string realmId,
            DateTime startDate,
            DateTime endDate,
            string summarizeColumnBy = "Month",
            string accountingMethod = "Accrual") =>
            GetReportAsync("BalanceSheet", accessToken, realmId, startDate, endDate, summarizeColumnBy, accountingMethod);

        private async Task<string> GetReportAsync(
            string reportType,
            string accessToken,
            string realmId,
            DateTime startDate,
            DateTime endDate,
            string summarizeColumnBy,
            string accountingMethod)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(realmId))
                throw new ArgumentException("Realm ID cannot be null or empty.", nameof(realmId));

            var requestUrl = _quickBooksOptions.RequestURL;
            if (string.IsNullOrWhiteSpace(requestUrl))
                throw new InvalidOperationException("QuickBooks:RequestURL configuration is missing or empty.");

            var client = _httpClientFactory.CreateClient(HttpClientName);

            // start_date/end_date must both be present — QBO silently ignores a lone end_date.
            var query =
                $"start_date={startDate:yyyy-MM-dd}" +
                $"&end_date={endDate:yyyy-MM-dd}" +
                $"&summarize_column_by={Uri.EscapeDataString(summarizeColumnBy)}" +
                $"&accounting_method={Uri.EscapeDataString(accountingMethod)}" +
                "&showrows=all&showcols=all" +
                "&format=json";

            if (!string.IsNullOrWhiteSpace(_quickBooksOptions.MinorVersion))
                query += $"&minorversion={Uri.EscapeDataString(_quickBooksOptions.MinorVersion)}";

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{requestUrl}/{realmId}/reports/{reportType}?{query}"
            );

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "QBO {ReportType} report request failed. StatusCode={StatusCode}, RealmId={RealmId}, Response={ResponseBody}",
                    reportType, response.StatusCode, realmId, content);
                throw new HttpRequestException(
                    $"QBO {reportType} report request failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={content}"
                );
            }

            _logger.LogDebug(
                "QBO {ReportType} report request completed. RealmId={RealmId}, StartDate={StartDate:yyyy-MM-dd}, EndDate={EndDate:yyyy-MM-dd}, SummarizeColumnBy={SummarizeColumnBy}",
                reportType, realmId, startDate, endDate, summarizeColumnBy);
            return content;
        }
    }
}
