using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Features.User;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IAppUserRepository _userRepo;

    public UserController(IAppUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _userRepo.GetByIdAsync(userId.Value);
        if (user == null) return NotFound();

        return Ok(ApiResponse<object>.Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Username,
            user.Email,
        }, "Profile fetched."));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        // Check username uniqueness (excluding self)
        var existing = await _userRepo.GetByUsernameAsync(request.Username);
        if (existing != null && existing.Id != userId.Value)
            return BadRequest(ApiResponse<object>.Fail("Username is already taken."));

        await _userRepo.UpdateProfileAsync(userId.Value, request.FirstName, request.LastName, request.Username);
        return Ok(ApiResponse<object>.Ok(null, "Profile updated successfully."));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _userRepo.GetByIdAsync(userId.Value);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            return BadRequest(ApiResponse<object>.Fail("Current password is incorrect."));

        var hashed = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepo.UpdatePasswordAsync(userId.Value, hashed);
        return Ok(ApiResponse<object>.Ok(null, "Password changed successfully."));
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst("UserId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}

public record UpdateProfileRequest(string FirstName, string LastName, string Username);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
