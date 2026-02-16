using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class JournalEntryController : ControllerBase
    {
        private readonly IJournalEntryService _journalEntryService;
        public JournalEntryController(IJournalEntryService journalEntryService)
        {
            _journalEntryService = journalEntryService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListJournalEntries([FromQuery] ListQueryParams? query = null)
        {
            query ??= new ListQueryParams();
            var result = await _journalEntryService.ListJournalEntriesAsync(query);
            return Ok(result);
        }

        [HttpGet("sync")]
        public async Task<IActionResult> SyncJournalEntries()
        {
            var result = await _journalEntryService.SyncJournalEntriesAsync();
            return Ok(result);
        }
    }
}
