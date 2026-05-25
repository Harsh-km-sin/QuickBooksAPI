using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.Features.Analytics.Queries;

namespace QuickBooksAPI.Features.Analytics;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public partial class AnalyticsController : ControllerBase
{
    private readonly AnalyticsQueries _queries;

    public AnalyticsController(AnalyticsQueries queries)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }
}
