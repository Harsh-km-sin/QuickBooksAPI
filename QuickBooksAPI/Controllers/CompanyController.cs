using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CompanyController : ControllerBase
    {
        private readonly ICurrentUser _currentUser;
        private readonly ISyncService _syncService;

        public CompanyController(ICurrentUser currentUser, ISyncService syncService)
        {
            _currentUser = currentUser;
            _syncService = syncService;
        }

        [HttpPost("sync/full")]
        public async Task<IActionResult> FullSync([FromBody] SyncRequestDto dto)
        {
            var userId = _currentUser.UserId;
            var realmId = _currentUser.RealmId;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(realmId))
                return Unauthorized(new { success = false, message = "User context is missing." });

            try
            {
                await _syncService.StartFullSyncAsync(realmId, userId);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }

            return Accepted(new
            {
                success = true,
                message = "Full sync queued successfully",
                companyId = realmId
            });
        }

        [HttpGet("sync/status")]
        public async Task<IActionResult> GetSyncStatus()
        {
            var realmId = _currentUser.RealmId;

            if (string.IsNullOrEmpty(realmId))
                return Unauthorized(new { success = false, message = "User context is missing." });

            var status = await _syncService.GetSyncStatusAsync(realmId);
            return Ok(new { success = true, data = status });
        }
    }
}
