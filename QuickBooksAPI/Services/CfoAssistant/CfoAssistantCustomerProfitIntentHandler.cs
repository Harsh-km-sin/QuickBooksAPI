using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Services.CfoAssistant;

public sealed class CfoAssistantCustomerProfitIntentHandler : ICfoAssistantIntentHandler
{
    private readonly ICustomerProfitabilityService _customerProfitabilityService;

    public CfoAssistantCustomerProfitIntentHandler(ICustomerProfitabilityService customerProfitabilityService)
    {
        _customerProfitabilityService = customerProfitabilityService ?? throw new ArgumentNullException(nameof(customerProfitabilityService));
    }

    public bool Matches(string questionLowerInvariant) =>
        CfoAssistantIntentMatching.ContainsAny(questionLowerInvariant,
            "unprofitable", "profitability", "customer profit", "worst customer");

    public async Task AppendAsync(int userId, string realmId, CfoAssistantContext context, CancellationToken cancellationToken = default)
    {
        var to = DateTime.UtcNow.Date;
        var from = to.AddMonths(-1);
        var cust = await _customerProfitabilityService.GetCustomerProfitabilityAsync(userId, realmId, from, to, 10, cancellationToken);
        var unprofitable = cust.Where(c => c.GrossMargin < 0).Take(5).ToList();
        if (unprofitable.Count > 0)
        {
            context.Narrative.AppendLine("Top unprofitable customers (by gross margin): ");
            foreach (var c in unprofitable)
                context.Narrative.AppendLine($"  {c.CustomerName}: Revenue ${c.Revenue:N2}, COGS ${c.CostOfGoods:N2}, Margin ${c.GrossMargin:N2} ({c.MarginPct:N1}%).");
        }
        else
            context.Narrative.AppendLine("No unprofitable customers in the last month (top 10 by revenue).");

        context.Citations.Add(new CitationDto
        {
            MetricName = "Customer Profitability",
            DateRange = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}",
            Endpoint = "/api/analytics/customer-profitability"
        });
    }
}
