using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using System.Security.Claims;

namespace QuickBooksAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IAuthService _authServices;

        public AuthController(IConfiguration config, IAuthService authServices)
        {
            _config = config;
            _authServices = authServices;
        }

        [HttpPost("SignUp")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserSignUpRequest request)
        {
            var response = await _authServices.RegisterUserAsync(request);
            return Ok(response);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var response = await _authServices.LoginUserAsync(request);
            return Ok(response);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // For stateless JWT, we rely on short token expiry.
            // This endpoint exists for:
            // 1. Audit logging (who logged out when)
            // 2. Future token blacklist support if needed
            // 3. Best practice client-server logout handshake
            
            var userIdClaim = User.FindFirst("UserId")?.Value;
            var userName = User.FindFirst("Name")?.Value ?? "Unknown";
            
            // Log the logout event (optional: add to audit log table)
            // _logger.LogInformation("User {UserId} ({UserName}) logged out.", userIdClaim, userName);
            
            return Ok(ApiResponse<string>.Ok("Logged out successfully.", "Logout complete."));
        }

        [HttpGet("oAuth")]
        public async Task<IActionResult> SignIn()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("User ID claim is missing or invalid.");
                }

                var authUrl = await _authServices.GenerateOAuthUrlAsync(userId);
                return Ok(ApiResponse<string>.Ok(authUrl, "Success"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while generating OAuth URL.");
            }
        }

        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, [FromQuery] string realmId)
        {
            var response = await _authServices.HandleCallbackAsync(code, state, realmId);
            var frontendBase = _config["QuickBooks:FrontendBaseUrl"]?.TrimEnd('/');
            if (!string.IsNullOrEmpty(frontendBase))
            {
                if (response.Success)
                    return Redirect($"{frontendBase}/?oauth=success");
                var message = Uri.EscapeDataString(response.Message ?? "Connection failed.");
                return Redirect($"{frontendBase}/?oauth=error&message={message}");
            }
            return Ok(response);
        }

        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect([FromBody] DisconnectQboRequest request)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized("User ID claim is missing or invalid.");

            if (request == null || string.IsNullOrWhiteSpace(request.RealmId))
                return BadRequest(ApiResponse<string>.Fail("RealmId is required."));

            var response = await _authServices.DisconnectQboAsync(userId, request.RealmId.Trim());
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("connected-companies")]
        public async Task<IActionResult> GetConnectedCompanies()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized("User ID claim is missing or invalid.");

            var response = await _authServices.GetConnectedCompaniesAsync(userId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
