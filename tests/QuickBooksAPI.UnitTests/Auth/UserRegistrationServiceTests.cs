using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Services.Auth;

namespace QuickBooksAPI.UnitTests.Auth;

public class UserRegistrationServiceTests
{
    [Fact]
    public async Task RegisterUserAsync_success_persists_user()
    {
        var userRepo = new Mock<IAppUserRepository>();
        userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        userRepo.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        userRepo.Setup(x => x.RegisterUserAsync(It.IsAny<AppUser>())).ReturnsAsync(42);

        var sut = new UserRegistrationService(
            userRepo.Object,
            new UserSignUpRequestValidator(),
            NullLogger<UserRegistrationService>.Instance);

        var req = new UserSignUpRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Username = "jane",
            Email = "jane@example.com",
            Password = "Abcd1234!"
        };

        var result = await sut.RegisterUserAsync(req);

        Assert.True(result.Success);
        Assert.Equal(42, result.Data);
        userRepo.Verify(x => x.RegisterUserAsync(It.Is<AppUser>(u => u.Email == "jane@example.com")), Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_duplicate_email_short_circuits()
    {
        var userRepo = new Mock<IAppUserRepository>();
        userRepo.Setup(x => x.GetByEmailAsync("taken@example.com")).ReturnsAsync(new AppUser { Id = 1, Email = "taken@example.com" });

        var sut = new UserRegistrationService(
            userRepo.Object,
            new UserSignUpRequestValidator(),
            NullLogger<UserRegistrationService>.Instance);

        var req = new UserSignUpRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Username = "jane",
            Email = "taken@example.com",
            Password = "Abcd1234!"
        };

        var result = await sut.RegisterUserAsync(req);

        Assert.False(result.Success);
        Assert.Contains("email", result.Message, StringComparison.OrdinalIgnoreCase);
        userRepo.Verify(x => x.RegisterUserAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUserAsync_uses_validator_before_repo()
    {
        var userRepo = new Mock<IAppUserRepository>();
        var validator = new Mock<IUserSignUpValidator>();
        validator.Setup(x => x.ValidateFields(It.IsAny<UserSignUpRequest>()))
            .Returns(ApiResponse<int>.Fail("bad"));

        var sut = new UserRegistrationService(
            userRepo.Object,
            validator.Object,
            NullLogger<UserRegistrationService>.Instance);

        var result = await sut.RegisterUserAsync(new UserSignUpRequest { FirstName = "x" });

        Assert.False(result.Success);
        Assert.Equal("bad", result.Message);
        userRepo.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }
}
