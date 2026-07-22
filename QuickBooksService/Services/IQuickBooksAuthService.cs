namespace QuickBooksService.Services
{
    public interface IQuickBooksAuthService
    {
        Task<string> HandleCallbackAsync(string code, string realmId);
        Task<string> RefreshTokenAsync(string refreshToken);
        /// <summary>Revokes the refresh token at Intuit. Returns true if revoke succeeded (2xx).</summary>
        Task<bool> DisconnectQboAsync(string refreshToken);
        /// <summary>Fetches company info JSON for the specified realm using the given access token.</summary>
        Task<string> GetCompanyInfoAsync(string accessToken, string realmId);
        /// <summary>Fetches company preferences JSON (includes <c>ReportPrefs.ReportBasis</c>) for the specified realm.</summary>
        Task<string> GetPreferencesAsync(string accessToken, string realmId);
    }
}
