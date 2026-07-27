using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Features.Terms
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TermController : ControllerBase
    {
        private readonly ITermService _termService;

        public TermController(ITermService termService)
        {
            _termService = termService ?? throw new ArgumentNullException(nameof(termService));
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out var id) ? id : 0;
        }

        private string? GetRealmId()
        {
            return Request.Headers["x-realm-id"].ToString() ?? Request.Headers["realmId"].ToString();
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListTerms([FromQuery] bool activeOnly = true)
        {
            var realmId = GetRealmId();
            if (string.IsNullOrWhiteSpace(realmId))
                return BadRequest(ApiResponse<object>.Fail("x-realm-id header is required."));

            var terms = await _termService.GetTermsAsync(realmId, activeOnly);
            return Ok(ApiResponse<object>.Ok(terms, "Terms retrieved successfully."));
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncTerms()
        {
            var userId = GetUserId();
            var realmId = GetRealmId();

            if (userId <= 0 || string.IsNullOrWhiteSpace(realmId))
                return BadRequest(ApiResponse<int>.Fail("Invalid user or missing x-realm-id header."));

            var result = await _termService.SyncTermsAsync(userId, realmId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
