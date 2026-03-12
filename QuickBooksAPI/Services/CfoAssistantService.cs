using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace QuickBooksAPI.Services
{
    public interface ICfoAssistantService
    {
        Task<CfoAssistantResponse> AskAsync(int userId, string realmId, string question, CancellationToken cancellationToken = default);
    }

    public class CfoAssistantResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<CitationDto> Citations { get; set; } = new();
    }

    public class CitationDto
    {
        public string MetricName { get; set; } = string.Empty;
        public string? DateRange { get; set; }
        public string? Endpoint { get; set; }
    }

    /// <summary>
    /// Answers CFO questions using warehouse-backed metrics; optionally uses Azure OpenAI to summarize.
    /// </summary>
    public class CfoAssistantService : ICfoAssistantService
    {
        private readonly ICashRunwayService _runwayService;
        private readonly IRevenueExpensesService _revenueExpensesService;
        private readonly ICustomerProfitabilityService _customerProfitabilityService;
        private readonly IVendorAnalyticsService _vendorAnalyticsService;
        private readonly IConfiguration _configuration;

        public CfoAssistantService(
            ICashRunwayService runwayService,
            IRevenueExpensesService revenueExpensesService,
            ICustomerProfitabilityService customerProfitabilityService,
            IVendorAnalyticsService vendorAnalyticsService,
            IConfiguration configuration)
        {
            _runwayService = runwayService;
            _revenueExpensesService = revenueExpensesService;
            _customerProfitabilityService = customerProfitabilityService;
            _vendorAnalyticsService = vendorAnalyticsService;
            _configuration = configuration;
        }

        public async Task<CfoAssistantResponse> AskAsync(int userId, string realmId, string question, CancellationToken cancellationToken = default)
        {
            var q = question.ToLowerInvariant().Trim();
            var context = new StringBuilder();
            var citations = new List<CitationDto>();

            if (ContainsAny(q, "runway", "cash runway", "months of runway", "how long", "burn"))
            {
                var runway = await _runwayService.GetRunwayAsync(userId, realmId, cancellationToken);
                context.AppendLine($"Cash runway: {runway.RunwayMonths} months. Current cash: ${runway.CurrentCash:N2}. Monthly burn: ${runway.MonthlyBurn:N2}. Expected revenue: ${runway.ExpectedRevenue:N2}.");
                citations.Add(new CitationDto { MetricName = "Cash Runway", Endpoint = "/api/analytics/cash-runway" });
            }

            if (ContainsAny(q, "revenue", "expenses", "last month", "vs previous", "revenue vs", "expenses vs"))
            {
                var to = DateTime.UtcNow.Date;
                var from = to.AddMonths(-2);
                var monthly = await _revenueExpensesService.GetMonthlyAsync(userId, realmId, from, to, cancellationToken);
                var ordered = monthly.OrderBy(m => m.MonthStart).ToList();
                foreach (var m in ordered)
                {
                    context.AppendLine($"Month {m.MonthStart:yyyy-MM}: Revenue ${m.Revenue:N2}, Expenses ${m.Expenses:N2}, Net ${m.Revenue - m.Expenses:N2}.");
                }
                citations.Add(new CitationDto { MetricName = "Revenue vs Expenses", DateRange = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}", Endpoint = "/api/analytics/revenue-expenses" });
            }

            if (ContainsAny(q, "unprofitable", "profitability", "customer profit", "worst customer"))
            {
                var to = DateTime.UtcNow.Date;
                var from = to.AddMonths(-1);
                var cust = await _customerProfitabilityService.GetCustomerProfitabilityAsync(userId, realmId, from, to, 10, cancellationToken);
                var unprofitable = cust.Where(c => c.GrossMargin < 0).Take(5).ToList();
                if (unprofitable.Count > 0)
                {
                    context.AppendLine("Top unprofitable customers (by gross margin): ");
                    foreach (var c in unprofitable)
                        context.AppendLine($"  {c.CustomerName}: Revenue ${c.Revenue:N2}, COGS ${c.CostOfGoods:N2}, Margin ${c.GrossMargin:N2} ({c.MarginPct:N1}%).");
                }
                else
                    context.AppendLine("No unprofitable customers in the last month (top 10 by revenue).");
                citations.Add(new CitationDto { MetricName = "Customer Profitability", DateRange = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}", Endpoint = "/api/analytics/customer-profitability" });
            }

            if (ContainsAny(q, "vendor", "top vendor", "spend"))
            {
                var top = await _vendorAnalyticsService.GetTopVendorsAsync(userId, realmId, 30, 5, cancellationToken);
                context.AppendLine("Top vendors by spend (last 30 days): ");
                foreach (var v in top)
                    context.AppendLine($"  {v.VendorName}: ${v.TotalSpend:N2} ({v.BillCount} bills).");
                citations.Add(new CitationDto { MetricName = "Vendor Spend", DateRange = "Last 30 days", Endpoint = "/api/analytics/vendor-spend/top" });
            }

            if (context.Length == 0)
            {
                return new CfoAssistantResponse
                {
                    Answer = "I can answer questions about cash runway, revenue vs expenses, top vendors by spend, and customer profitability. Try asking e.g. 'How many months of runway do we have?' or 'Top unprofitable customers?'",
                    Citations = new List<CitationDto>()
                };
            }

            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            var apiKey = _configuration["AzureOpenAI:ApiKey"];
            var deployment = _configuration["AzureOpenAI:DeploymentName"] ?? "gpt-35-turbo";

            if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var answer = await CallAzureOpenAIAsync(endpoint, apiKey, deployment, context.ToString(), question, cancellationToken);
                    return new CfoAssistantResponse { Answer = answer, Citations = citations };
                }
                catch (Exception)
                {
                    return new CfoAssistantResponse
                    {
                        Answer = "Summary from your data:\n\n" + context.ToString().Replace("\n", "\n• ").TrimStart('•', ' '),
                        Citations = citations
                    };
                }
            }

            return new CfoAssistantResponse
            {
                Answer = "Based on your data:\n\n" + context.ToString().Replace("\n", "\n• ").TrimStart('•', ' '),
                Citations = citations
            };
        }

        private static bool ContainsAny(string text, params string[] terms)
        {
            return terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<string> CallAzureOpenAIAsync(string endpoint, string apiKey, string deployment, string context, string question, CancellationToken cancellationToken)
        {
            var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/chat/completions?api-version=2024-02-15-preview";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            var body = new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a CFO assistant. Answer ONLY using the following data. Do not invent any numbers. Be concise (2-4 sentences)." },
                    new { role = "user", content = $"Data:\n{context}\n\nQuestion: {question}" }
                },
                max_tokens = 300,
                temperature = 0.2
            };
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
                return "I couldn't generate a response.";
            var message = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "No content.";
            return message.Trim();
        }
    }
}
