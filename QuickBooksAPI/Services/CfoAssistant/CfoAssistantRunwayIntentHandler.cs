using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Services.CfoAssistant;

public sealed class CfoAssistantRunwayIntentHandler : ICfoAssistantIntentHandler
{
    private readonly ICashRunwayService _runwayService;

    public CfoAssistantRunwayIntentHandler(ICashRunwayService runwayService)
    {
        _runwayService = runwayService ?? throw new ArgumentNullException(nameof(runwayService));
    }

    public bool Matches(string questionLowerInvariant) =>
        CfoAssistantIntentMatching.ContainsAny(questionLowerInvariant,
            "runway", "cash runway", "months of runway", "how long", "burn");

    public async Task AppendAsync(int userId, string realmId, CfoAssistantContext context, CancellationToken cancellationToken = default)
    {
        var runway = await _runwayService.GetRunwayAsync(userId, realmId, cancellationToken);
        context.Narrative.AppendLine($"Cash runway: {runway.RunwayMonths} months. Current cash: ${runway.CurrentCash:N2}. Monthly burn: ${runway.MonthlyBurn:N2}. Expected revenue: ${runway.ExpectedRevenue:N2}.");
        context.Citations.Add(new CitationDto { MetricName = "Cash Runway", Endpoint = "/api/analytics/cash-runway" });
    }
}
