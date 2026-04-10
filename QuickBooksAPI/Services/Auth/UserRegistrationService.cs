using Microsoft.Extensions.Logging;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Services.Auth;

public class UserRegistrationService : IUserRegistrationService
{
    private readonly IAppUserRepository _userRepo;
    private readonly IUserSignUpValidator _signUpValidator;
    private readonly ILogger<UserRegistrationService> _logger;

    public UserRegistrationService(
        IAppUserRepository userRepo,
        IUserSignUpValidator signUpValidator,
        ILogger<UserRegistrationService> logger)
    {
        _userRepo = userRepo;
        _signUpValidator = signUpValidator;
        _logger = logger;
    }

    public async Task<ApiResponse<int>> RegisterUserAsync(UserSignUpRequest request)
    {
        try
        {
            request.FirstName = request.FirstName?.Trim() ?? string.Empty;
            request.LastName = request.LastName?.Trim() ?? string.Empty;
            request.Username = request.Username?.Trim() ?? string.Empty;
            request.Email = request.Email?.Trim() ?? string.Empty;

            var fieldError = _signUpValidator.ValidateFields(request);
            if (fieldError != null)
                return fieldError;

            var existingUserByEmail = await _userRepo.GetByEmailAsync(request.Email);
            if (existingUserByEmail != null)
                return ApiResponse<int>.Fail("An account with this email already exists.");

            var existingUserByUsername = await _userRepo.GetByUsernameAsync(request.Username);
            if (existingUserByUsername != null)
                return ApiResponse<int>.Fail("This username is already taken.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new AppUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Username = request.Username,
                Email = request.Email,
                Password = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            var userId = await _userRepo.RegisterUserAsync(user);

            if (userId > 0)
                return ApiResponse<int>.Ok(userId, "User registered successfully.");

            return ApiResponse<int>.Fail("Registration failed.", new[] { "Unable to create user account. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User registration failed.");
            return ApiResponse<int>.Fail("Registration failed.", new[] { ex.Message });
        }
    }
}
