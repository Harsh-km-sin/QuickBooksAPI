using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksShared.Options;

namespace QuickBooksAPI.Services.Auth;

public class UserLoginService : IUserLoginService
{
    private readonly IAppUserRepository _userRepo;
    private readonly ITokenRepository _tokenRepo;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<UserLoginService> _logger;

    public UserLoginService(
        IAppUserRepository userRepo,
        ITokenRepository tokenRepo,
        IOptions<JwtOptions> jwtOptions,
        ILogger<UserLoginService> logger)
    {
        _userRepo = userRepo;
        _tokenRepo = tokenRepo;
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _logger = logger;
    }

    public async Task<ApiResponse<string>> LoginUserAsync(UserLoginRequest request)
    {
        try
        {
            var user = await _userRepo.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return ApiResponse<string>.Fail("Login failed.", new[] { "User not found with the provided email address." });
            }

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            if (!isPasswordValid)
            {
                return ApiResponse<string>.Fail("Login failed.", new[] { "Invalid password provided." });
            }

            var token = await GenerateJwtTokenAsync(user);
            return ApiResponse<string>.Ok(token, "Login successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed.");
            return ApiResponse<string>.Fail("Login failed.", new[] { ex.Message });
        }
    }

    private async Task<string> GenerateJwtTokenAsync(AppUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var realmIds = await _tokenRepo.GetRealmIdsByUserIdAsync(user.Id);
        var realmIdsJson = JsonSerializer.Serialize(realmIds);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("UserId", user.Id.ToString()),
            new Claim("UserName", user.Username ?? string.Empty),
            new Claim("Name", user.FirstName ?? string.Empty),
            new Claim("RealmIds", realmIdsJson),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
