using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

/// <summary>Field-level sign-up validation (before uniqueness / persistence).</summary>
public interface IUserSignUpValidator
{
    /// <summary>Returns a failed <see cref="ApiResponse{T}"/> if invalid; null if fields are acceptable.</summary>
    ApiResponse<int>? ValidateFields(UserSignUpRequest request);
}
