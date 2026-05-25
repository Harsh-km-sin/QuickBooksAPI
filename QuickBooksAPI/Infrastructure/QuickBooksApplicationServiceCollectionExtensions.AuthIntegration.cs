using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Integrations.QuickBooks;
using QuickBooksAPI.Services;
using QuickBooksAPI.Services.Auth;

namespace QuickBooksAPI.Infrastructure;

public static partial class QuickBooksApplicationServiceCollectionExtensions
{
    private static IServiceCollection AddQuickBooksAuthAndIntegrationApplicationServices(this IServiceCollection services)
    {
        services.AddAccountingProviders();

        services.AddSingleton<IUserSignUpValidator, UserSignUpRequestValidator>();
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IUserLoginService, UserLoginService>();
        services.AddScoped<IQboConnectionService, QboConnectionService>();
        services.AddScoped<IQboTokenLifecycleService, QboTokenLifecycleService>();
        services.AddScoped<IConnectedCompanyQueryService, ConnectedCompanyQueryService>();
        services.AddScoped<IAuthService, AuthServices>();
        return services;
    }
}
