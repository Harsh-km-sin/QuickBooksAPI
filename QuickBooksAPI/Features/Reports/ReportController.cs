using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Reports;

namespace QuickBooksAPI.Features.Reports;

/// <summary>
/// Financial statements served from stored data. No endpoint here calls QuickBooks except
/// <see cref="SyncReports"/> — reads are answered entirely from our database.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// P&amp;L for a month-aligned range. Partial months are snapped outwards to whole months,
    /// since stored columns are monthly.
    /// </summary>
    /// <param name="useFiscalYear">
    /// When true, ignores <paramref name="startDate"/> and returns the fiscal year containing
    /// <paramref name="endDate"/> (or today). Boundaries come from the company's stored
    /// FiscalYearStartMonth — no QuickBooks call.
    /// </param>
    [HttpGet("profit-and-loss")]
    public async Task<IActionResult> GetProfitAndLoss(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool useFiscalYear = false,
        [FromQuery] AccountingMethod? accountingMethod = null)
    {
        var response = await _reportService.GetProfitAndLossAsync(startDate, endDate, useFiscalYear, accountingMethod);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Balance Sheet as of a date — the latest stored month-end at or before it. Point-in-time by
    /// nature, so there is no range parameter: values are never summed across months.
    /// </summary>
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet(
        [FromQuery] DateTime? asOfDate = null,
        [FromQuery] AccountingMethod? accountingMethod = null)
    {
        var response = await _reportService.GetBalanceSheetAsync(asOfDate, accountingMethod);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>Which periods are resident, so the UI can bound its date picker.</summary>
    [HttpGet("periods")]
    public async Task<IActionResult> GetSyncedPeriods([FromQuery] string reportType = ReportTypes.ProfitAndLoss)
    {
        var response = await _reportService.GetSyncedPeriodsAsync(reportType);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Manual re-pull from QuickBooks. Backs the "Generate Reports" action; always forces a pull
    /// rather than honouring the skip-if-nothing-changed check the Full Sync path uses.
    /// </summary>
    [HttpGet("sync")]
    public async Task<IActionResult> SyncReports()
    {
        var response = await _reportService.SyncReportsAsync();
        return Ok(response);
    }
}
