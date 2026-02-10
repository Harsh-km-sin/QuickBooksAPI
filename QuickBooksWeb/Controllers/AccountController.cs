using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksWeb.Services;

namespace QuickBooksWeb.Controllers;

public class AccountController : Controller
{
    private readonly IQuickBooksApiClient _apiClient;

    public AccountController(IQuickBooksApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null, CancellationToken ct = default)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Email and password are required.");
            return View();
        }

        var result = await _apiClient.LoginAsync(email, password);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View();
        }

        if (result.Data == null)
        {
            ModelState.AddModelError("", "Login failed.");
            return View();
        }

        var realmId = QuickBooksApiClient.ExtractRealmIdFromToken(result.Data);
        _apiClient.SetToken(result.Data, realmId ?? "");

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, email),
            new(System.Security.Claims.ClaimTypes.Email, email)
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return LocalRedirect(returnUrl ?? "/Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string firstName, string lastName, string username, string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "All fields are required.");
            return View();
        }

        var result = await _apiClient.RegisterAsync(firstName, lastName, username, email, password);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View();
        }

        var loginResult = await _apiClient.LoginAsync(email, password);
        if (loginResult.Success && loginResult.Data != null)
        {
            var realmId = QuickBooksApiClient.ExtractRealmIdFromToken(loginResult.Data);
            _apiClient.SetToken(loginResult.Data, realmId ?? "");
            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.Name, email),
                new(System.Security.Claims.ClaimTypes.Email, email)
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(identity));
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        _apiClient.ClearToken();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
