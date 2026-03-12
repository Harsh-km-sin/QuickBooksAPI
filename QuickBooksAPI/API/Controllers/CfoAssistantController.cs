using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Services;

namespace QuickBooksAPI.API.Controllers
{
    [ApiController]
    [Route("api/cfo-assistant")]
    [Authorize]
    public class CfoAssistantController : ControllerBase
    {
        private readonly ICfoAssistantService _assistantService;
        private readonly ICurrentUser _currentUser;

        public CfoAssistantController(ICfoAssistantService assistantService, ICurrentUser currentUser)
        {
            _assistantService = assistantService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Ask a question and get an answer grounded in warehouse-backed metrics.
        /// </summary>
        [HttpPost("ask")]
        public async Task<ActionResult<QuickBooksAPI.Services.CfoAssistantResponse>> Ask([FromBody] CfoAssistantRequest request)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(_currentUser.RealmId))
                return Unauthorized();
            if (!int.TryParse(_currentUser.UserId, out var userId))
                return Unauthorized();
            if (string.IsNullOrWhiteSpace(request?.Question))
                return BadRequest(new { message = "Question is required." });

            var response = await _assistantService.AskAsync(userId, _currentUser.RealmId, request.Question.Trim(), HttpContext.RequestAborted);
            return Ok(response);
        }
    }

    public class CfoAssistantRequest
    {
        public string Question { get; set; } = string.Empty;
    }
}
