using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksWeb.Services;

namespace QuickBooksWeb.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly IQuickBooksApiClient _apiClient;

    public ProductsController(IQuickBooksApiClient apiClient)
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

        var result = await _apiClient.ListProductsAsync();
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return View(Array.Empty<Models.Product>());
        }
        return View(result.Data ?? Array.Empty<Models.Product>());
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

        var result = await _apiClient.SyncProductsAsync();
        if (result.Success)
        {
            TempData["Success"] = $"Synced {result.Data} products from QuickBooks.";
        }
        else
        {
            TempData["Error"] = result.Message;
        }
        return RedirectToAction("Index");
    }
}
