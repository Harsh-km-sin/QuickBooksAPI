using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IAppUserRepository
    {
        Task<int> RegisterUserAsync(AppUser user);
        Task<AppUser?> GetByEmailAsync(string email);
        Task<AppUser?> GetByUsernameAsync(string username);
        Task<bool> UserExistsAsync(int userId);

    }

}
