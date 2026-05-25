using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Features.ChartOfAccounts.Handlers;

namespace QuickBooksAPI.Features.ChartOfAccounts;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ChartOfAccountsController : ControllerBase
{
    private readonly ListChartOfAccountsHandler _listChartOfAccounts;
    private readonly SyncChartOfAccountsHandler _syncChartOfAccounts;

    public ChartOfAccountsController(
        ListChartOfAccountsHandler listChartOfAccounts,
        SyncChartOfAccountsHandler syncChartOfAccounts)
    {
        _listChartOfAccounts = listChartOfAccounts;
        _syncChartOfAccounts = syncChartOfAccounts;
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListChartOfAccounts([FromQuery] ListQueryParams? query = null)
    {
        query ??= new ListQueryParams();
        var result = await _listChartOfAccounts.HandlePagedAsync(query);
        return Ok(result);
    }

    [HttpGet("sync")]
    public async Task<IActionResult> SyncChartOfAccounts()
    {
        var result = await _syncChartOfAccounts.HandleAsync();
        return Ok(result);
    }
}
