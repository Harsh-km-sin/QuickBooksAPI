using System.Net.Mail;
using System.Text.RegularExpressions;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Services.Auth;

public sealed class UserSignUpRequestValidator : IUserSignUpValidator
{
    private static readonly Regex NameRegex = new(@"^[A-Za-z]+$", RegexOptions.Compiled);
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9._]+$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,100}$", RegexOptions.Compiled);

    public ApiResponse<int>? ValidateFields(UserSignUpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
            return ApiResponse<int>.Fail("First Name is required.");

        if (request.FirstName.Length > 50)
            return ApiResponse<int>.Fail("First Name cannot exceed 50 characters.");

        if (!NameRegex.IsMatch(request.FirstName))
            return ApiResponse<int>.Fail("First Name can only contain letters.");

        if (string.IsNullOrWhiteSpace(request.LastName))
            return ApiResponse<int>.Fail("Last Name is required.");

        if (request.LastName.Length > 50)
            return ApiResponse<int>.Fail("Last Name cannot exceed 50 characters.");

        if (!NameRegex.IsMatch(request.LastName))
            return ApiResponse<int>.Fail("Last Name can only contain letters.");

        if (string.IsNullOrWhiteSpace(request.Username))
            return ApiResponse<int>.Fail("Username is required.");

        if (request.Username.Length < 3 || request.Username.Length > 30)
            return ApiResponse<int>.Fail("Username must be between 3 and 30 characters.");

        if (!UsernameRegex.IsMatch(request.Username))
            return ApiResponse<int>.Fail("Username can only contain letters, numbers, dots, and underscores.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return ApiResponse<int>.Fail("Email is required.");

        if (!IsValidEmail(request.Email))
            return ApiResponse<int>.Fail("Please enter a valid email address.");

        if (string.IsNullOrWhiteSpace(request.Password))
            return ApiResponse<int>.Fail("Password is required.");

        if (!PasswordRegex.IsMatch(request.Password))
        {
            return ApiResponse<int>.Fail(
                "Registration failed.",
                new[] { "Password must be 8-100 characters and include at least 1 uppercase, 1 lowercase, 1 number, and 1 special character." });
        }

        return null;
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
