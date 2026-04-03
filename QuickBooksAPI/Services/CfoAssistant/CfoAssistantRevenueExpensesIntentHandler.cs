using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Services.CfoAssistant;

public sealed class CfoAssistantRevenueExpensesIntentHandler : ICfoAssistantIntentHandler
{
    private readonly IRevenueExpensesService _revenueExpensesService;

    public CfoAssistantRevenueExpensesIntentHandler(IRevenueExpensesService revenueExpensesService)
    {
        _revenueExpensesService = revenueExpensesService ?? throw new ArgumentNullException(nameof(revenueExpensesService));
    }

    public bool Matches(string questionLowerInvariant) =>
        CfoAssistantIntentMatching.ContainsAny(questionLowerInvariant,
            "revenue", "expenses", "last month", "vs previous", "revenue vs", "expenses vs");

    public async Task AppendAsync(int userId, string realmId, CfoAssistantContext context, CancellationToken cancellationToken = default)
    {
        var to = DateTime.UtcNow.Date;
        var from = to.AddMonths(-2);
        var monthly = await _revenueExpensesService.GetMonthlyAsync(userId, realmId, from, to, cancellationToken);
        var ordered = monthly.OrderBy(m => m.MonthStart).ToList();
        foreach (var m in ordered)
        {
            context.Narrative.AppendLine($"Month {m.MonthStart:yyyy-MM}: Revenue ${m.Revenue:N2}, Expenses ${m.Expenses:N2}, Net ${m.Revenue - m.Expenses:N2}.");
        }

        context.Citations.Add(new CitationDto
        {
            MetricName = "Revenue vs Expenses",
            DateRange = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}",
            Endpoint = "/api/analytics/revenue-expenses"
        });
    }
}
