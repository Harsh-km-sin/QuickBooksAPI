using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksWeb.Services;

namespace QuickBooksWeb.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly IQuickBooksApiClient _apiClient;

    public CustomersController(IQuickBooksApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        if (!_apiClient.IsAuthenticated)
        {
            TempData["Error"] = "Please sign in and connect QuickBooks.";
            return RedirectToAction("Index", "Home");
        }

        var result = await _apiClient.ListCustomersAsync();
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return View(Array.Empty<Models.Customer>());
        }
        return View(result.Data ?? Array.Empty<Models.Customer>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync()
    {
        if (!_apiClient.IsAuthenticated)
        {
            TempData["Error"] = "Please sign in and connect QuickBooks.";
            return RedirectToAction("Index");
        }

        var result = await _apiClient.SyncCustomersAsync();
        if (result.Success)
        {
            TempData["Success"] = $"Synced {result.Data} customers from QuickBooks.";
        }
        else
        {
            TempData["Error"] = result.Message;
        }
        return RedirectToAction("Index");
    }
}
