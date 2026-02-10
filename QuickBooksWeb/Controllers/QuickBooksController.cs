using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksWeb.Services;

namespace QuickBooksWeb.Controllers;

[Authorize]
public class QuickBooksController : Controller
{
    private readonly IQuickBooksApiClient _apiClient;

    public QuickBooksController(IQuickBooksApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Connect()
    {
        var result = await _apiClient.GetOAuthUrlAsync();
        if (!result.Success || string.IsNullOrEmpty(result.Data))
        {
            TempData["Error"] = result.Message ?? "Failed to get QuickBooks authorization URL.";
            return RedirectToAction("Index", "Home");
        }
        return Redirect(result.Data!);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, [FromQuery] string realmId)
    {
        var result = await _apiClient.HandleOAuthCallbackAsync(code, state, realmId);
        if (result.Success)
        {
            _apiClient.SetRealmId(realmId);
            TempData["Success"] = "QuickBooks connected successfully.";
        }
        else
        {
            TempData["Error"] = result.Message ?? "QuickBooks connection failed.";
        }
        return RedirectToAction("Index", "Home");
    }
}
