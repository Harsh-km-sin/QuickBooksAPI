using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Controllers;

[ApiController]
[Route("api/cfo-assistant")]
[Authorize]
public class CfoAssistantController : ControllerBase
{
    private readonly ICfoAssistantService _assistantService;
    private readonly IRequestContext _requestContext;

    public CfoAssistantController(ICfoAssistantService assistantService, IRequestContext requestContext)
    {
        _assistantService = assistantService;
        _requestContext = requestContext;
    }

    /// <summary>
    /// Ask a question and get an answer grounded in warehouse-backed metrics.
    /// </summary>
    [HttpPost("ask")]
    public async Task<ActionResult<CfoAssistantResponse>> Ask([FromBody] CfoAssistantRequest request)
    {
        if (string.IsNullOrWhiteSpace(_requestContext.UserId) || string.IsNullOrWhiteSpace(_requestContext.RealmId))
            return Unauthorized();
        if (!int.TryParse(_requestContext.UserId, out var userId))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Question))
            return BadRequest(new { message = "Question is required." });

        var response = await _assistantService.AskAsync(userId, _requestContext.RealmId, request.Question.Trim(), HttpContext.RequestAborted);
        return Ok(response);
    }
}
