using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Services.Auth;

namespace QuickBooksAPI.UnitTests.Auth;

public class UserSignUpRequestValidatorTests
{
    private readonly UserSignUpRequestValidator _sut = new();

    private static UserSignUpRequest ValidRequest() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Username = "jane.doe",
        Email = "jane@example.com",
        Password = "Abcd1234!"
    };

    [Fact]
    public void ValidateFields_valid_request_returns_null()
    {
        Assert.Null(_sut.ValidateFields(ValidRequest()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateFields_first_name_required(string firstName)
    {
        var r = ValidRequest();
        r.FirstName = firstName;
        var err = _sut.ValidateFields(r);
        Assert.NotNull(err);
        Assert.False(err.Success);
        Assert.Contains("First Name", err.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateFields_first_name_letters_only()
    {
        var r = ValidRequest();
        r.FirstName = "J0hn";
        var err = _sut.ValidateFields(r);
        Assert.NotNull(err);
        Assert.False(err.Success);
    }

    [Fact]
    public void ValidateFields_password_complexity()
    {
        var r = ValidRequest();
        r.Password = "short1!";
        var err = _sut.ValidateFields(r);
        Assert.NotNull(err);
        Assert.False(err.Success);
        Assert.NotNull(err.Errors);
    }

    [Fact]
    public void ValidateFields_invalid_email()
    {
        var r = ValidRequest();
        r.Email = "not-an-email";
        var err = _sut.ValidateFields(r);
        Assert.NotNull(err);
        Assert.False(err.Success);
    }

    [Fact]
    public void ValidateFields_username_bad_chars()
    {
        var r = ValidRequest();
        r.Username = "bad name";
        var err = _sut.ValidateFields(r);
        Assert.NotNull(err);
        Assert.False(err.Success);
    }
}
