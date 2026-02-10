using QuickBooksWeb.Models;

namespace QuickBooksWeb.Services;

public interface IQuickBooksApiClient
{
    Task<ApiResponse<string>> LoginAsync(string email, string password);
    Task<ApiResponse<int>> RegisterAsync(string firstName, string lastName, string username, string email, string password);
    Task<ApiResponse<string>> GetOAuthUrlAsync();
    Task<ApiResponse<object>> HandleOAuthCallbackAsync(string code, string state, string realmId);
    Task<ApiResponse<IEnumerable<Product>>> ListProductsAsync();
    Task<ApiResponse<int>> SyncProductsAsync();
    Task<ApiResponse<IEnumerable<Customer>>> ListCustomersAsync();
    Task<ApiResponse<int>> SyncCustomersAsync();
    void SetToken(string token, string realmId);
    void SetRealmId(string realmId);
    void ClearToken();
    bool IsAuthenticated { get; }
}
