using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs;
using QuickBooksAPI.Services.Auth;
using QuickBooksService.Services;
using QuickBooksShared.Options;

namespace QuickBooksAPI.UnitTests.Auth;

public class QboConnectionServiceTests
{
    private static IOptions<QuickBooksOptions> TestQboOptions() =>
        Options.Create(new QuickBooksOptions
        {
            AuthUrl = "https://auth.example/oauth",
            ClientId = "cid",
            RedirectUri = "https://app/cb",
            Scopes = "com.intuit.quickbooks.accounting"
        });

    [Fact]
    public async Task GenerateOAuthUrlAsync_invalid_user_throws()
    {
        var userRepo = new Mock<IAppUserRepository>();
        userRepo.Setup(x => x.UserExistsAsync(99)).ReturnsAsync(false);

        var sut = new QboConnectionService(
            Mock.Of<IQuickBooksAuthService>(),
            Mock.Of<ITokenRepository>(),
            userRepo.Object,
            Mock.Of<ICompanyRepository>(),
            TestQboOptions(),
            NullLogger<QboConnectionService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.GenerateOAuthUrlAsync(99));
    }

    [Fact]
    public async Task GenerateOAuthUrlAsync_returns_url_with_state()
    {
        var userRepo = new Mock<IAppUserRepository>();
        userRepo.Setup(x => x.UserExistsAsync(3)).ReturnsAsync(true);

        var sut = new QboConnectionService(
            Mock.Of<IQuickBooksAuthService>(),
            Mock.Of<ITokenRepository>(),
            userRepo.Object,
            Mock.Of<ICompanyRepository>(),
            TestQboOptions(),
            NullLogger<QboConnectionService>.Instance);

        var url = await sut.GenerateOAuthUrlAsync(3);

        Assert.Contains("client_id=cid", url, StringComparison.Ordinal);
        Assert.Contains("state=3_", url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleCallbackAsync_happy_path_saves_token_and_company()
    {
        const int userId = 5;
        const string realm = "r123";
        var state = $"{userId}_{Guid.NewGuid():N}";

        var tokenDto = new TokenResponseDto
        {
            AccessToken = "acc",
            RefreshToken = "ref",
            IdToken = "idt",
            ExpiresIn = 3600,
            TokenType = "bearer"
        };
        var tokenJson = JsonSerializer.Serialize(tokenDto);

        var qbo = new Mock<IQuickBooksAuthService>();
        qbo.Setup(x => x.HandleCallbackAsync("code", realm)).ReturnsAsync(tokenJson);
        qbo.Setup(x => x.GetCompanyInfoAsync("acc", realm)).ReturnsAsync("{}");

        var tokenRepo = new Mock<ITokenRepository>();
        var userRepo = new Mock<IAppUserRepository>();
        userRepo.Setup(x => x.UserExistsAsync(userId)).ReturnsAsync(true);

        var companyRepo = new Mock<ICompanyRepository>();

        var sut = new QboConnectionService(
            qbo.Object,
            tokenRepo.Object,
            userRepo.Object,
            companyRepo.Object,
            TestQboOptions(),
            NullLogger<QboConnectionService>.Instance);

        var result = await sut.HandleCallbackAsync("code", state, realm);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        tokenRepo.Verify(x => x.SaveTokenAsync(It.Is<QuickBooksToken>(t =>
            t.UserId == userId && t.RealmId == realm && t.AccessToken == "acc")), Times.Once);
        companyRepo.Verify(x => x.UpsertCompanyAsync(It.Is<Company>(c =>
            c.UserId == userId && c.QboRealmId == realm && c.QboAccessToken == "acc")), Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_invalid_state_fails()
    {
        var sut = new QboConnectionService(
            Mock.Of<IQuickBooksAuthService>(),
            Mock.Of<ITokenRepository>(),
            Mock.Of<IAppUserRepository>(),
            Mock.Of<ICompanyRepository>(),
            TestQboOptions(),
            NullLogger<QboConnectionService>.Instance);

        var result = await sut.HandleCallbackAsync("c", "badstate", "realm");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DisconnectQboAsync_clears_local_data()
    {
        var token = new QuickBooksToken
        {
            Id = 10,
            UserId = 2,
            RealmId = "rx",
            RefreshToken = "rt",
            AccessToken = "a",
            IdToken = "i",
            TokenType = "bearer",
            ExpiresIn = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(x => x.GetTokenByUserAndRealmAsync(2, "rx")).ReturnsAsync(token);

        var qbo = new Mock<IQuickBooksAuthService>();
        qbo.Setup(x => x.DisconnectQboAsync("rt")).ReturnsAsync(true);

        var companyRepo = new Mock<ICompanyRepository>();

        var sut = new QboConnectionService(
            qbo.Object,
            tokenRepo.Object,
            Mock.Of<IAppUserRepository>(),
            companyRepo.Object,
            TestQboOptions(),
            NullLogger<QboConnectionService>.Instance);

        var result = await sut.DisconnectQboAsync(2, "rx");

        Assert.True(result.Success);
        tokenRepo.Verify(x => x.DeleteTokenAsync(10), Times.Once);
        companyRepo.Verify(x => x.ClearCompanyTokenAsync(2, "rx"), Times.Once);
    }

    [Fact]
    public async Task DisconnectQboAsync_missing_realm_fails()
    {
        var sut = new QboConnectionService(
            Mock.Of<IQuickBooksAuthService>(),
            Mock.Of<ITokenRepository>(),
            Mock.Of<IAppUserRepository>(),
            Mock.Of<ICompanyRepository>(),
            TestQboOptions(),
            NullLogger<QboConnectionService>.Instance);

        var result = await sut.DisconnectQboAsync(1, "  ");

        Assert.False(result.Success);
    }
}
