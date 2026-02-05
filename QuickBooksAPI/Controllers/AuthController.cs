using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
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
                return Ok(authUrl);
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
            return Ok(response);
        }
    }
}
