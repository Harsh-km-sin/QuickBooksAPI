using Moq;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Services.Auth;

namespace QuickBooksAPI.UnitTests.Auth;

public class AuthServicesFacadeTests
{
    [Fact]
    public async Task Delegates_to_slices()
    {
        var registration = new Mock<IUserRegistrationService>();
        var login = new Mock<IUserLoginService>();
        var qbo = new Mock<IQboConnectionService>();
        var lifecycle = new Mock<IQboTokenLifecycleService>();
        var companies = new Mock<IConnectedCompanyQueryService>();

        registration.Setup(x => x.RegisterUserAsync(It.IsAny<UserSignUpRequest>()))
            .ReturnsAsync(ApiResponse<int>.Ok(1));
        login.Setup(x => x.LoginUserAsync(It.IsAny<UserLoginRequest>()))
            .ReturnsAsync(ApiResponse<string>.Ok("jwt"));
        qbo.Setup(x => x.GenerateOAuthUrlAsync(2)).ReturnsAsync("url");
        qbo.Setup(x => x.HandleCallbackAsync("c", "s", "r"))
            .ReturnsAsync(ApiResponse<QuickBooksToken>.Ok(new QuickBooksToken()));
        lifecycle.Setup(x => x.IsTokenExpiredAsync(null)).ReturnsAsync(true);
        lifecycle.Setup(x => x.RefreshTokenIfExpiredAsync(1, "r")).ReturnsAsync((QboAccessTokenSnapshot?)null);
        qbo.Setup(x => x.DisconnectQboAsync(1, "r"))
            .ReturnsAsync(ApiResponse<string>.Ok("ok"));
        companies.Setup(x => x.GetConnectedCompaniesAsync(1))
            .ReturnsAsync(ApiResponse<IEnumerable<ConnectedCompanyDto>>.Ok(Array.Empty<ConnectedCompanyDto>()));

        var sut = new AuthServices(
            registration.Object,
            login.Object,
            qbo.Object,
            lifecycle.Object,
            companies.Object);

        await sut.RegisterUserAsync(new UserSignUpRequest());
        await sut.LoginUserAsync(new UserLoginRequest());
        await sut.GenerateOAuthUrlAsync(2);
        await sut.HandleCallbackAsync("c", "s", "r");
        await sut.IsTokenExpiredAsync(null);
        await sut.RefreshTokenIfExpiredAsync(1, "r");
        await sut.DisconnectQboAsync(1, "r");
        await sut.GetConnectedCompaniesAsync(1);

        registration.Verify(x => x.RegisterUserAsync(It.IsAny<UserSignUpRequest>()), Times.Once);
        login.Verify(x => x.LoginUserAsync(It.IsAny<UserLoginRequest>()), Times.Once);
        qbo.Verify(x => x.GenerateOAuthUrlAsync(2), Times.Once);
        qbo.Verify(x => x.HandleCallbackAsync("c", "s", "r"), Times.Once);
        lifecycle.Verify(x => x.IsTokenExpiredAsync(null), Times.Once);
        lifecycle.Verify(x => x.RefreshTokenIfExpiredAsync(1, "r"), Times.Once);
        qbo.Verify(x => x.DisconnectQboAsync(1, "r"), Times.Once);
        companies.Verify(x => x.GetConnectedCompaniesAsync(1), Times.Once);
    }
}
