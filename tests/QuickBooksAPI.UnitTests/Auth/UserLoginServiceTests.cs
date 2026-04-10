using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Services.Auth;
using QuickBooksShared.Options;

namespace QuickBooksAPI.UnitTests.Auth;

public class UserLoginServiceTests
{
    private static IOptions<JwtOptions> TestJwtOptions() =>
        Options.Create(new JwtOptions
        {
            Key = "01234567890123456789012345678901",
            Issuer = "unit-test-issuer",
            Audience = "unit-test-audience"
        });

    [Fact]
    public async Task LoginUserAsync_wrong_password_fails()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectHorse1!");
        var userRepo = new Mock<IAppUserRepository>();
        userRepo.Setup(x => x.GetByEmailAsync("a@b.com")).ReturnsAsync(new AppUser
        {
            Id = 1,
            Username = "u",
            Email = "a@b.com",
            Password = hash
        });

        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(x => x.GetRealmIdsByUserIdAsync(1)).ReturnsAsync(Array.Empty<string>());

        var sut = new UserLoginService(userRepo.Object, tokenRepo.Object, TestJwtOptions(), NullLogger<UserLoginService>.Instance);

        var result = await sut.LoginUserAsync(new UserLoginRequest { Email = "a@b.com", Password = "wrong" });

        Assert.False(result.Success);
        Assert.Contains("password", string.Join(" ", result.Errors ?? Array.Empty<string>()), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginUserAsync_valid_credentials_returns_jwt()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectHorse1!");
        var userRepo = new Mock<IAppUserRepository>();
        userRepo.Setup(x => x.GetByEmailAsync("a@b.com")).ReturnsAsync(new AppUser
        {
            Id = 7,
            Username = "sam",
            FirstName = "Sam",
            Email = "a@b.com",
            Password = hash
        });

        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(x => x.GetRealmIdsByUserIdAsync(7)).ReturnsAsync(new[] { "realm-1" });

        var sut = new UserLoginService(userRepo.Object, tokenRepo.Object, TestJwtOptions(), NullLogger<UserLoginService>.Instance);

        var result = await sut.LoginUserAsync(new UserLoginRequest { Email = "a@b.com", Password = "CorrectHorse1!" });

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Data));
        Assert.Contains(".", result.Data!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginUserAsync_unknown_user_fails()
    {
        var userRepo = new Mock<IAppUserRepository>();
        userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

        var sut = new UserLoginService(
            userRepo.Object,
            Mock.Of<ITokenRepository>(),
            TestJwtOptions(),
            NullLogger<UserLoginService>.Instance);

        var result = await sut.LoginUserAsync(new UserLoginRequest { Email = "nope@example.com", Password = "x" });

        Assert.False(result.Success);
    }
}
