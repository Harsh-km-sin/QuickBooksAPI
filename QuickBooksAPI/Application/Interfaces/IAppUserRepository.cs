using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IAppUserRepository
{
    Task<int> RegisterUserAsync(AppUser user);
    Task<AppUser?> GetByEmailAsync(string email);
    Task<AppUser?> GetByUsernameAsync(string username);
    Task<AppUser?> GetByIdAsync(int userId);
    Task<bool> UserExistsAsync(int userId);
    Task UpdateProfileAsync(int userId, string firstName, string lastName, string username);
    Task UpdatePasswordAsync(int userId, string hashedPassword);
}
