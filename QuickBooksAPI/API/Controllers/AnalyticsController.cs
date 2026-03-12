using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Services;

namespace QuickBooksAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly ICashRunwayService _cashRunwayService;
        private readonly IVendorAnalyticsService _vendorAnalyticsService;
        private readonly ICustomerProfitabilityService _customerProfitabilityService;
        private readonly IRevenueExpensesService _revenueExpensesService;
        private readonly IAnomalyEventRepository _anomalyEventRepository;
        private readonly IKpiService _kpiService;
        private readonly IForecastService _forecastService;
        private readonly ICloseIssueService _closeIssueService;
        private readonly IDimEntityRepository _dimEntityRepository;
        private readonly IConsolidatedPnlRepository _consolidatedPnlRepository;
        private readonly ICurrentUser _currentUser;

        public AnalyticsController(
            ICashRunwayService cashRunwayService,
            IVendorAnalyticsService vendorAnalyticsService,
            ICustomerProfitabilityService customerProfitabilityService,
            IRevenueExpensesService revenueExpensesService,
            IAnomalyEventRepository anomalyEventRepository,
            IKpiService kpiService,
            IForecastService forecastService,
            ICloseIssueService closeIssueService,
            IDimEntityRepository dimEntityRepository,
            IConsolidatedPnlRepository consolidatedPnlRepository,
            ICurrentUser currentUser)
        {
            _cashRunwayService = cashRunwayService;
            _vendorAnalyticsService = vendorAnalyticsService;
            _customerProfitabilityService = customerProfitabilityService;
            _revenueExpensesService = revenueExpensesService;
            _anomalyEventRepository = anomalyEventRepository;
            _kpiService = kpiService;
            _forecastService = forecastService;
            _closeIssueService = closeIssueService;
            _dimEntityRepository = dimEntityRepository;
            _consolidatedPnlRepository = consolidatedPnlRepository;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Returns a simple cash runway calculation for the current company.
        /// </summary>
        [HttpGet("cash-runway")]
        public async Task<ActionResult<ApiResponse<CashRunwayResult>>> GetCashRunway()
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
            {
                return Unauthorized(ApiResponse<CashRunwayResult>.Fail("User or realm context is missing."));
            }

            if (!int.TryParse(_currentUser.UserId, out var userId))
            {
                return Unauthorized(ApiResponse<CashRunwayResult>.Fail("Invalid user id."));
            }

            var result = await _cashRunwayService.GetRunwayAsync(userId, _currentUser.RealmId);
            return Ok(ApiResponse<CashRunwayResult>.Ok(result, "Cash runway calculated."));
        }

        /// <summary>
        /// Returns top vendors by spend for the current company (e.g. period=30 or 90 days).
        /// </summary>
        [HttpGet("vendor-spend/top")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<VendorSpendDto>>>> GetVendorSpendTop(
            [FromQuery] int period = 30,
            [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized(ApiResponse<IReadOnlyList<VendorSpendDto>>.Fail("User or realm context is missing."));
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<IReadOnlyList<VendorSpendDto>>.Fail("Invalid user id."));

            var data = await _vendorAnalyticsService.GetTopVendorsAsync(userId, _currentUser.RealmId, Math.Max(1, period), Math.Clamp(limit, 1, 100));
            return Ok(ApiResponse<IReadOnlyList<VendorSpendDto>>.Ok(data, "Top vendors by spend."));
        }

        /// <summary>
        /// Returns vendor spend summary for the current company over a date range.
        /// </summary>
        [HttpGet("vendor-spend/summary")]
        public async Task<ActionResult<ApiResponse<VendorSpendSummaryDto>>> GetVendorSpendSummary(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized(ApiResponse<VendorSpendSummaryDto>.Fail("User or realm context is missing."));
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<VendorSpendSummaryDto>.Fail("Invalid user id."));

            var toDate = to?.Date ?? DateTime.UtcNow.Date;
            var fromDate = from?.Date ?? toDate.AddMonths(-1);
            if (fromDate > toDate)
                return BadRequest(ApiResponse<VendorSpendSummaryDto>.Fail("From must be before or equal to To."));

            var data = await _vendorAnalyticsService.GetSummaryAsync(userId, _currentUser.RealmId, fromDate, toDate);
            return Ok(ApiResponse<VendorSpendSummaryDto>.Ok(data, "Vendor spend summary."));
        }

        /// <summary>
        /// Returns customer profitability for the current company over a date range.
        /// </summary>
        [HttpGet("customer-profitability")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>>> GetCustomerProfitability(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int top = 50)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized(ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Fail("User or realm context is missing."));
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Fail("Invalid user id."));

            var toDate = to?.Date ?? DateTime.UtcNow.Date;
            var fromDate = from?.Date ?? toDate.AddMonths(-1);
            if (fromDate > toDate)
                return BadRequest(ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Fail("From must be before or equal to To."));

            var data = await _customerProfitabilityService.GetCustomerProfitabilityAsync(userId, _currentUser.RealmId, fromDate, toDate, Math.Clamp(top, 1, 200));
            return Ok(ApiResponse<IReadOnlyList<CustomerProfitabilityDto>>.Ok(data, "Customer profitability."));
        }

        /// <summary>
        /// Returns monthly revenue and expenses for the current company (e.g. last 12 months for charts).
        /// </summary>
        [HttpGet("revenue-expenses")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>>> GetRevenueExpenses(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized(ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Fail("User or realm context is missing."));
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Fail("Invalid user id."));

            var toDate = to?.Date ?? DateTime.UtcNow.Date;
            var fromDate = from?.Date ?? toDate.AddMonths(-12);
            if (fromDate > toDate)
                return BadRequest(ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Fail("From must be before or equal to To."));

            var data = await _revenueExpensesService.GetMonthlyAsync(userId, _currentUser.RealmId, fromDate, toDate);
            return Ok(ApiResponse<IReadOnlyList<RevenueExpensesMonthlyDto>>.Ok(data, "Revenue vs expenses by month."));
        }

        /// <summary>
        /// Returns anomaly events for the current company, optionally filtered by since date.
        /// </summary>
        [HttpGet("anomalies")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<AnomalyDto>>>> GetAnomalies(
            [FromQuery] DateTime? since = null)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized(ApiResponse<IReadOnlyList<AnomalyDto>>.Fail("User or realm context is missing."));
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<IReadOnlyList<AnomalyDto>>.Fail("Invalid user id."));

            var events = await _anomalyEventRepository.GetByUserAndRealmAsync(userId, _currentUser.RealmId, since);
            var data = events.Select(e => new AnomalyDto
            {
                Id = e.Id,
                Type = e.Type,
                Severity = e.Severity,
                Details = e.Details,
                DetectedAt = e.DetectedAt
            }).ToList();
            return Ok(ApiResponse<IReadOnlyList<AnomalyDto>>.Ok(data, "Anomaly events."));
        }

        /// <summary>
        /// Returns KPI snapshot history for the current company (e.g. for sparklines). Optional filter by names.
        /// </summary>
        [HttpGet("kpis")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<KpiSnapshotDto>>>> GetKpis(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? names = null)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized(ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Fail("User or realm context is missing."));
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Fail("Invalid user id."));

            var toDate = to?.Date ?? DateTime.UtcNow.Date;
            var fromDate = from?.Date ?? toDate.AddMonths(-6);
            if (fromDate > toDate)
                return BadRequest(ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Fail("From must be before or equal to To."));

            IReadOnlyList<string>? nameList = null;
            if (!string.IsNullOrWhiteSpace(names))
                nameList = names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var data = await _kpiService.GetKpisAsync(userId, _currentUser.RealmId, fromDate, toDate, nameList);
            return Ok(ApiResponse<IReadOnlyList<KpiSnapshotDto>>.Ok(data, "KPI snapshots."));
        }

        /// <summary>
        /// Creates a forecast scenario and runs deterministic projection; returns scenario id.
        /// </summary>
        [HttpPost("forecast")]
        public async Task<ActionResult<ApiResponse<ForecastScenarioDto>>> PostForecast([FromBody] CreateForecastRequest request)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized(ApiResponse<ForecastScenarioDto>.Fail("User or realm context is missing."));
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<ForecastScenarioDto>.Fail("Invalid user id."));
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(ApiResponse<ForecastScenarioDto>.Fail("Name is required."));

            var scenarioId = await _forecastService.CreateAndComputeAsync(
                userId, _currentUser.RealmId, request.Name,
                Math.Clamp(request.HorizonMonths, 1, 60),
                request.AssumptionsJson, null, default);

            var detail = await _forecastService.GetForecastAsync(scenarioId, userId, _currentUser.RealmId, default);
            return Ok(ApiResponse<ForecastScenarioDto>.Ok(detail!.Scenario, "Forecast scenario created and computed."));
        }

        /// <summary>
        /// Returns a forecast scenario and its results by id.
        /// </summary>
        [HttpGet("forecast/{id:int}")]
        public async Task<ActionResult<ApiResponse<ForecastDetailDto>>> GetForecast(int id)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized(ApiResponse<ForecastDetailDto>.Fail("User or realm context is missing."));
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<ForecastDetailDto>.Fail("Invalid user id."));

            var detail = await _forecastService.GetForecastAsync(id, userId, _currentUser.RealmId, default);
            if (detail == null)
                return NotFound(ApiResponse<ForecastDetailDto>.Fail("Forecast scenario not found."));
            return Ok(ApiResponse<ForecastDetailDto>.Ok(detail, "Forecast detail."));
        }

        /// <summary>
        /// Returns close and data-quality issues for the current company.
        /// </summary>
        [HttpGet("close-issues")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<CloseIssueDto>>>> GetCloseIssues(
            [FromQuery] DateTime? since = null,
            [FromQuery] string? severity = null,
            [FromQuery] bool unresolvedOnly = true)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized(ApiResponse<IReadOnlyList<CloseIssueDto>>.Fail("User or realm context is missing."));
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<IReadOnlyList<CloseIssueDto>>.Fail("Invalid user id."));

            var data = await _closeIssueService.GetIssuesAsync(userId, _currentUser.RealmId, since, severity, unresolvedOnly, default);
            return Ok(ApiResponse<IReadOnlyList<CloseIssueDto>>.Ok(data, "Close and data-quality issues."));
        }

        /// <summary>
        /// Marks a close issue as resolved.
        /// </summary>
        [HttpPost("close-issues/{id:int}/resolve")]
        public async Task<ActionResult<ApiResponse<object>>> ResolveCloseIssue(int id)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized(ApiResponse<object>.Fail("User or realm context is missing."));
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<object>.Fail("Invalid user id."));

            await _closeIssueService.ResolveAsync(id, userId, _currentUser.RealmId, default);
            return Ok(ApiResponse<object>.Ok(null, "Issue marked as resolved."));
        }

        /// <summary>
        /// Returns entities (companies/subsidiaries) for the current user for consolidation.
        /// </summary>
        [HttpGet("entities")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EntityDto>>>> GetEntities()
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || !int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<IReadOnlyList<EntityDto>>.Fail("User context is missing."));

            var entities = await _dimEntityRepository.GetByUserIdAsync(userId, default);
            var data = entities.Select(e => new EntityDto { Id = e.Id, Name = e.Name, RealmId = e.RealmId, IsConsolidatedNode = e.IsConsolidatedNode }).ToList();
            return Ok(ApiResponse<IReadOnlyList<EntityDto>>.Ok(data, "Entities for consolidation."));
        }

        /// <summary>
        /// Returns consolidated P&L for a parent entity (must belong to current user).
        /// </summary>
        [HttpGet("consolidated-pnl")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>>> GetConsolidatedPnl(
            [FromQuery] int entityId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || !int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized(ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Fail("User context is missing."));

            var entities = await _dimEntityRepository.GetByUserIdAsync(userId, default);
            var entity = entities.FirstOrDefault(e => e.Id == entityId);
            if (entity == null)
                return NotFound(ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Fail("Entity not found."));

            var toDate = to?.Date ?? DateTime.UtcNow.Date;
            var fromDate = from?.Date ?? toDate.AddMonths(-12);
            if (fromDate > toDate)
                return BadRequest(ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Fail("From must be before or equal to To."));

            var rows = await _consolidatedPnlRepository.GetByEntityAndRangeAsync(entityId, fromDate, toDate, default);
            var data = rows.Select(r => new ConsolidatedPnlRowDto
            {
                PeriodStart = r.PeriodStart,
                PeriodEnd = r.PeriodEnd,
                Revenue = r.Revenue,
                Expenses = r.Expenses,
                NetIncome = r.NetIncome
            }).ToList();
            return Ok(ApiResponse<IReadOnlyList<ConsolidatedPnlRowDto>>.Ok(data, "Consolidated P&L."));
        }
    }
}

