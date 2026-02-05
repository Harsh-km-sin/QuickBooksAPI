namespace QuickBooksService.Services
{
    public interface IQuickBooksAuthService
    {
        public Task<string> HandleCallbackAsync(string code, string realmId);
        public Task<string> RefreshTokenAsync(string refreshToken);
    }
}
