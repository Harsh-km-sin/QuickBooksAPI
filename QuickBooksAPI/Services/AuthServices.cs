using Microsoft.IdentityModel.Tokens;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksService.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Mail;

namespace QuickBooksAPI.Services
{
    public class AuthServices : IAuthService
    {
        private readonly IQuickBooksAuthService _quickBooksAuthService;
        private readonly ITokenRepository _tokenRepo;
        private readonly IAppUserRepository _userRepo;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthServices> _logger;

        public AuthServices(IQuickBooksAuthService quickBooksAuthService, ITokenRepository tokenRepo, IConfiguration config, IAppUserRepository userRepo, ILogger<AuthServices> logger)
        {
            _quickBooksAuthService = quickBooksAuthService;
            _tokenRepo = tokenRepo;
            _config = config;
            _userRepo = userRepo;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GenerateOAuthUrlAsync(int userId)
        {
            var userExists = await _userRepo.UserExistsAsync(userId);
            if (!userExists)
                throw new ArgumentException("Invalid user ID.");

            var clientId = _config["QuickBooks:ClientId"];
            var redirectUri = _config["QuickBooks:RedirectUri"];
            var scope = _config["QuickBooks:Scopes"];
            var url = _config["QuickBooks:AuthUrl"];
            var state = $"{userId}_{Guid.NewGuid():N}";

            var authUrl = $"{url}" +
                          $"?client_id={clientId}" +
                          $"&redirect_uri={redirectUri}" +
                          $"&response_type=code" +
                          $"&scope={scope}" +
                          $"&state={state}";

            return authUrl;
        }
        public async Task<ApiResponse<int>> RegisterUserAsync(UserSignUpRequest request)
        {
            try
            {
                // 1. Sanitize Inputs
                request.FirstName = request.FirstName?.Trim();
                request.LastName = request.LastName?.Trim();
                request.Username = request.Username?.Trim();
                request.Email = request.Email?.Trim();

                // 2. Validate Required Fields & Lengths
                if (string.IsNullOrWhiteSpace(request.FirstName))
                    return ApiResponse<int>.Fail("First Name is required.");

                if (request.FirstName.Length > 50)
                    return ApiResponse<int>.Fail("First Name cannot exceed 50 characters.");

                if (string.IsNullOrWhiteSpace(request.LastName))
                    return ApiResponse<int>.Fail("Last Name is required.");

                if (request.LastName.Length > 50)
                    return ApiResponse<int>.Fail("Last Name cannot exceed 50 characters.");

                if (string.IsNullOrWhiteSpace(request.Username))
                    return ApiResponse<int>.Fail("Username is required.");

                if (request.Username.Length < 3 || request.Username.Length > 30)
                    return ApiResponse<int>.Fail("Username must be between 3 and 30 characters.");

                if (!Regex.IsMatch(request.Username, @"^[a-zA-Z0-9._]+$"))
                    return ApiResponse<int>.Fail("Username can only contain letters, numbers, dots, and underscores.");

                if (string.IsNullOrWhiteSpace(request.Email))
                    return ApiResponse<int>.Fail("Email is required.");

                if (!IsValidEmail(request.Email))
                    return ApiResponse<int>.Fail("Please enter a valid email address.");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return ApiResponse<int>.Fail("Password is required.");

                // 3. Password Complexity Check
                var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,100}$");
                if (!passwordRegex.IsMatch(request.Password))
                {
                    return ApiResponse<int>.Fail(
                        "Registration failed.",
                        new[] { "Password must be 8-100 characters and include at least 1 uppercase, 1 lowercase, 1 number, and 1 special character." }
                    );
                }

                // 4. Check for Uniqueness (Production grade: check before attempt)
                var existingUserByEmail = await _userRepo.GetByEmailAsync(request.Email);
                if (existingUserByEmail != null)
                    return ApiResponse<int>.Fail("An account with this email already exists.");

                var existingUserByUsername = await _userRepo.GetByUsernameAsync(request.Username);
                if (existingUserByUsername != null)
                    return ApiResponse<int>.Fail("This username is already taken.");

                // 5. Hash Password & Save
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
                {
                    return ApiResponse<int>.Ok(userId, "User registered successfully.");
                }
                
                return ApiResponse<int>.Fail("Registration failed.", new[] { "Unable to create user account. Please try again." });
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Fail("Registration failed.", new[] { ex.Message });
            }
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

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
                if (!isPasswordValid)
                {
                    return ApiResponse<string>.Fail("Login failed.", new[] { "Invalid password provided." });
                }

                var token = await GenerateJwtTokenAsync(user);
                return ApiResponse<string>.Ok(token, "Login successful.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Login failed.", new[] { ex.Message });
            }
        }
        private async Task<string> GenerateJwtTokenAsync(AppUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Get all realmIds for the user
            var realmIds = await _tokenRepo.GetRealmIdsByUserIdAsync(user.Id);
            var realmIdsJson = JsonSerializer.Serialize(realmIds);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserId", user.Id.ToString()),
                new Claim("RealmIds", realmIdsJson),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), 
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<ApiResponse<QuickBooksToken>> HandleCallbackAsync(string code, string state, string realmId)
        {
            try
            {
                var stateParts = state?.Split('_');
                if (stateParts == null || stateParts.Length != 2)
                    return ApiResponse<QuickBooksToken>.Fail("QuickBooks authentication failed.", new[] { "Invalid state parameter received from QuickBooks." });

                if (!int.TryParse(stateParts[0], out var userId))
                    return ApiResponse<QuickBooksToken>.Fail("QuickBooks authentication failed.", new[] { "Invalid user ID in state parameter." });

                var userExists = await _userRepo.UserExistsAsync(userId);
                if (!userExists)
                    return ApiResponse<QuickBooksToken>.Fail("QuickBooks authentication failed.", new[] { $"User with ID {userId} does not exist." });

                var existingToken = await _tokenRepo.GetTokenByUserAndRealmAsync(userId, realmId);
                if (existingToken != null)
                {
                    await _tokenRepo.DeleteTokenAsync(existingToken.Id);
                }
                var tokenJson = await _quickBooksAuthService.HandleCallbackAsync(code, realmId);
                var tokenDto = JsonSerializer.Deserialize<TokenResponseDto>(tokenJson);

                if (tokenDto == null)
                    return ApiResponse<QuickBooksToken>.Fail("QuickBooks authentication failed.", new[] { "Failed to retrieve authentication tokens from QuickBooks." });

                var token = new QuickBooksToken
                {
                    UserId = userId,
                    RealmId = realmId,
                    IdToken = tokenDto.IdToken ?? string.Empty,
                    AccessToken = tokenDto.AccessToken ?? string.Empty,
                    RefreshToken = tokenDto.RefreshToken ?? string.Empty,
                    TokenType = tokenDto.TokenType ?? "bearer",
                    ExpiresIn = tokenDto.ExpiresIn,
                    XRefreshTokenExpiresIn = tokenDto.XRefreshTokenExpiresIn,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _tokenRepo.SaveTokenAsync(token);

                return ApiResponse<QuickBooksToken>.Ok(token, "QuickBooks token saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "QuickBooks authentication failed during callback.");
                return ApiResponse<QuickBooksToken>.Fail("QuickBooks authentication failed. Please try again or reconnect your account.");
            }
        }

        /// <summary>
        /// Checks if the QuickBooks access token has expired.
        /// Token is expired if CreatedAt + ExpiresIn (in seconds) is less than current UTC time.
        /// </summary>
        public async Task<bool> IsTokenExpiredAsync(QuickBooksToken? token)
        {
            if (token == null)
                return true; // Treat null token as expired

            // Calculate expiration time: CreatedAt + ExpiresIn seconds
            var expirationTime = token.CreatedAt.AddSeconds(token.ExpiresIn);
            
            // Add a 1-minute buffer to refresh tokens slightly before they actually expire
            var bufferTime = expirationTime.AddMinutes(-1);
            
            return DateTime.UtcNow >= bufferTime;
        }

        /// <summary>
        /// Checks if token is expired, and if so, refreshes it using the refresh token.
        /// Returns the updated token (or original if not expired).
        /// Returns null if refresh fails.
        /// </summary>
        public async Task<QuickBooksToken?> RefreshTokenIfExpiredAsync(int userId, string realmId)
        {
            try
            {
                // Get the current token
                var token = await _tokenRepo.GetTokenByUserAndRealmAsync(userId, realmId);
                if (token == null)
                    return null; // No token found

                // Check if token is expired
                if (!await IsTokenExpiredAsync(token))
                    return token; // Token is still valid, return as-is

                // Token is expired, refresh it
                var refreshResponseJson = await _quickBooksAuthService.RefreshTokenAsync(token.RefreshToken);
                var refreshResponse = JsonSerializer.Deserialize<TokenResponseDto>(refreshResponseJson);

                if (refreshResponse == null)
                    return null; // Failed to parse refresh response

                // Update the token with new values
                token.IdToken = refreshResponse.IdToken ?? token.IdToken;
                token.AccessToken = refreshResponse.AccessToken ?? token.AccessToken;
                token.RefreshToken = refreshResponse.RefreshToken ?? token.RefreshToken; // QBO may return new refresh token
                token.TokenType = refreshResponse.TokenType ?? token.TokenType;
                token.ExpiresIn = refreshResponse.ExpiresIn;
                token.XRefreshTokenExpiresIn = refreshResponse.XRefreshTokenExpiresIn;
                token.CreatedAt = DateTime.UtcNow; // Reset CreatedAt to current time
                token.UpdatedAt = DateTime.UtcNow;

                // Update token in database
                await _tokenRepo.UpdateTokenAsync(token);

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token refresh failed. UserId={UserId}, RealmId={RealmId}", userId, realmId);
                return null;
            }
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
    