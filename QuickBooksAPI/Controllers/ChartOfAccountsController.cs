using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;
using System.Threading.Tasks;

namespace QuickBooksAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChartOfAccountsController : ControllerBase
    {
        private readonly IChartOfAccountsService _chartOfAccountsServices;
        public ChartOfAccountsController(IChartOfAccountsService chartOfAccountsServices)
        {
            _chartOfAccountsServices = chartOfAccountsServices;
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListChartOfAccounts([FromQuery] ListQueryParams? query = null)
        {
            query ??= new ListQueryParams();
            var result = await _chartOfAccountsServices.ListChartOfAccountsAsync(query);
            return Ok(result);
        }

        [HttpGet("sync")]
        public async Task<IActionResult> SyncChartOfAccounts()
        {
            var result = await _chartOfAccountsServices.syncChartOfAccounts();
            return Ok(result);
        }
    }
}
