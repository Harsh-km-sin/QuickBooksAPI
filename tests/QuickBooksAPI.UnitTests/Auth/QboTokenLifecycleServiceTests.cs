using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.Services.Auth;
using QuickBooksService.Services;

namespace QuickBooksAPI.UnitTests.Auth;

public class QboTokenLifecycleServiceTests
{
    [Fact]
    public async Task IsTokenExpiredAsync_null_is_expired()
    {
        var sut = new QboTokenLifecycleService(
            Mock.Of<IQuickBooksAuthService>(),
            Mock.Of<ITokenRepository>(),
            Mock.Of<ICompanyRepository>(),
            NullLogger<QboTokenLifecycleService>.Instance);

        Assert.True(await sut.IsTokenExpiredAsync(null));
    }

    [Fact]
    public async Task IsTokenExpiredAsync_fresh_token_not_expired()
    {
        var sut = new QboTokenLifecycleService(
            Mock.Of<IQuickBooksAuthService>(),
            Mock.Of<ITokenRepository>(),
            Mock.Of<ICompanyRepository>(),
            NullLogger<QboTokenLifecycleService>.Instance);

        var token = new QuickBooksToken
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresIn = 3600
        };

        Assert.False(await sut.IsTokenExpiredAsync(token));
    }

    [Fact]
    public async Task RefreshTokenIfExpiredAsync_when_valid_returns_without_refresh()
    {
        var token = new QuickBooksToken
        {
            Id = 1,
            UserId = 9,
            RealmId = "r",
            AccessToken = "a",
            RefreshToken = "r",
            IdToken = "i",
            TokenType = "bearer",
            CreatedAt = DateTime.UtcNow,
            ExpiresIn = 7200,
            UpdatedAt = DateTime.UtcNow
        };

        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(x => x.GetTokenByUserAndRealmAsync(9, "r")).ReturnsAsync(token);

        var qbo = new Mock<IQuickBooksAuthService>();

        var sut = new QboTokenLifecycleService(
            qbo.Object,
            tokenRepo.Object,
            Mock.Of<ICompanyRepository>(),
            NullLogger<QboTokenLifecycleService>.Instance);

        var result = await sut.RefreshTokenIfExpiredAsync(9, "r");

        Assert.Same(token, result);
        qbo.Verify(x => x.RefreshTokenAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokenIfExpiredAsync_when_expired_updates_repo_and_company()
    {
        var token = new QuickBooksToken
        {
            Id = 2,
            UserId = 9,
            RealmId = "r",
            AccessToken = "old",
            RefreshToken = "rt",
            IdToken = "i",
            TokenType = "bearer",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresIn = 60,
            UpdatedAt = DateTime.UtcNow
        };

        var refreshed = new TokenResponseDto
        {
            AccessToken = "newacc",
            RefreshToken = "newrt",
            ExpiresIn = 3600,
            TokenType = "bearer"
        };

        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(x => x.GetTokenByUserAndRealmAsync(9, "r")).ReturnsAsync(token);

        var qbo = new Mock<IQuickBooksAuthService>();
        qbo.Setup(x => x.RefreshTokenAsync("rt")).ReturnsAsync(JsonSerializer.Serialize(refreshed));

        var companyRepo = new Mock<ICompanyRepository>();

        var sut = new QboTokenLifecycleService(
            qbo.Object,
            tokenRepo.Object,
            companyRepo.Object,
            NullLogger<QboTokenLifecycleService>.Instance);

        var result = await sut.RefreshTokenIfExpiredAsync(9, "r");

        Assert.NotNull(result);
        Assert.Equal("newacc", result!.AccessToken);
        tokenRepo.Verify(x => x.UpdateTokenAsync(It.Is<QuickBooksToken>(t => t.AccessToken == "newacc")), Times.Once);
        companyRepo.Verify(x => x.UpsertCompanyAsync(It.Is<Company>(c =>
            c.UserId == 9 && c.QboRealmId == "r" && c.QboAccessToken == "newacc")), Times.Once);
    }
}
