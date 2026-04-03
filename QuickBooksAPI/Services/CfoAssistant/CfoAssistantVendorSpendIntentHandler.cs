using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Services.CfoAssistant;

public sealed class CfoAssistantVendorSpendIntentHandler : ICfoAssistantIntentHandler
{
    private readonly IVendorAnalyticsService _vendorAnalyticsService;

    public CfoAssistantVendorSpendIntentHandler(IVendorAnalyticsService vendorAnalyticsService)
    {
        _vendorAnalyticsService = vendorAnalyticsService ?? throw new ArgumentNullException(nameof(vendorAnalyticsService));
    }

    public bool Matches(string questionLowerInvariant) =>
        CfoAssistantIntentMatching.ContainsAny(questionLowerInvariant,
            "vendor", "top vendor", "spend");

    public async Task AppendAsync(int userId, string realmId, CfoAssistantContext context, CancellationToken cancellationToken = default)
    {
        var top = await _vendorAnalyticsService.GetTopVendorsAsync(userId, realmId, 30, 5, cancellationToken);
        context.Narrative.AppendLine("Top vendors by spend (last 30 days): ");
        foreach (var v in top)
            context.Narrative.AppendLine($"  {v.VendorName}: ${v.TotalSpend:N2} ({v.BillCount} bills).");

        context.Citations.Add(new CitationDto
        {
            MetricName = "Vendor Spend",
            DateRange = "Last 30 days",
            Endpoint = "/api/analytics/vendor-spend/top"
        });
    }
}
