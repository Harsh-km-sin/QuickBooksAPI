using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QuickBooksWeb.Models;

namespace QuickBooksWeb.Services;

public class QuickBooksApiClient : IQuickBooksApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string TokenKey = "QuickBooksToken";
    private const string RealmIdKey = "QuickBooksRealmId";

    public QuickBooksApiClient(HttpClient httpClient, IOptions<ApiSettings> options, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(GetStoredToken());

    private static CookieOptions GetCookieOptions(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddHours(2)
    };

    public void SetToken(string token, string realmId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            var opts = GetCookieOptions(context.Request.IsHttps);
            context.Response.Cookies.Append(TokenKey, token, opts);
            context.Response.Cookies.Append(RealmIdKey, realmId, opts);
        }
    }

    public void SetRealmId(string realmId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            var opts = GetCookieOptions(context.Request.IsHttps);
            context.Response.Cookies.Append(RealmIdKey, realmId, opts);
        }
    }

    public void ClearToken()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            context.Response.Cookies.Delete(TokenKey);
            context.Response.Cookies.Delete(RealmIdKey);
        }
    }

    private string? GetStoredToken() => _httpContextAccessor.HttpContext?.Request.Cookies[TokenKey];
    private string? GetStoredRealmId() => _httpContextAccessor.HttpContext?.Request.Cookies[RealmIdKey];

    private void ConfigureRequest()
    {
        var token = GetStoredToken();
        var realmId = GetStoredRealmId();
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Remove("X-Realm-Id");
        if (!string.IsNullOrEmpty(token))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrEmpty(realmId))
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Realm-Id", realmId);
    }

    public async Task<ApiResponse<string>> LoginAsync(string email, string password)
    {
        var payload = new { email, password };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/auth/login", content);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<string>>(json, JsonOptions) ?? new ApiResponse<string> { Success = false, Message = "Login failed" };
    }

    public static string? ExtractRealmIdFromToken(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            var bytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(bytes);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("RealmIds", out var realmIdsEl))
            {
                var realmIds = JsonSerializer.Deserialize<string[]>(realmIdsEl.GetRawText());
                return realmIds?.FirstOrDefault();
            }
        }
        catch { }
        return null;
    }

    public async Task<ApiResponse<int>> RegisterAsync(string firstName, string lastName, string username, string email, string password)
    {
        var payload = new { firstName, lastName, username, email, password };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/auth/SignUp", content);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<int>>(json, JsonOptions) ?? new ApiResponse<int> { Success = false };
    }

    public async Task<ApiResponse<string>> GetOAuthUrlAsync()
    {
        ConfigureRequest();
        var response = await _httpClient.GetAsync("api/auth/oAuth");
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<string>>(json, JsonOptions) ?? new ApiResponse<string> { Success = false };
    }

    public async Task<ApiResponse<object>> HandleOAuthCallbackAsync(string code, string state, string realmId)
    {
        var response = await _httpClient.GetAsync($"api/auth/callback?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}&realmId={Uri.EscapeDataString(realmId)}");
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<object>>(json, JsonOptions) ?? new ApiResponse<object> { Success = false };
    }

    public async Task<ApiResponse<IEnumerable<Product>>> ListProductsAsync()
    {
        ConfigureRequest();
        var response = await _httpClient.GetAsync("api/product/list");
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<IEnumerable<Product>>>(json, JsonOptions) ?? new ApiResponse<IEnumerable<Product>> { Success = false };
    }

    public async Task<ApiResponse<int>> SyncProductsAsync()
    {
        ConfigureRequest();
        var response = await _httpClient.GetAsync("api/product/sync");
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<int>>(json, JsonOptions) ?? new ApiResponse<int> { Success = false };
    }

    public async Task<ApiResponse<IEnumerable<Customer>>> ListCustomersAsync()
    {
        ConfigureRequest();
        var response = await _httpClient.GetAsync("api/customer/list");
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<IEnumerable<Customer>>>(json, JsonOptions) ?? new ApiResponse<IEnumerable<Customer>> { Success = false };
    }

    public async Task<ApiResponse<int>> SyncCustomersAsync()
    {
        ConfigureRequest();
        var response = await _httpClient.GetAsync("api/customer/sync");
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<int>>(json, JsonOptions) ?? new ApiResponse<int> { Success = false };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = "https://localhost:7135";
}
