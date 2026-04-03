using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuickBooksShared.Options;
using System;
using Microsoft.Extensions.Options;

namespace QuickBooksShared;

public static class QuickBooksOptionsServiceCollectionExtensions
{
    public static IServiceCollection AddQuickBooksTypedOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        bool validateOnStart)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // QuickBooks OAuth + API endpoint settings
        var quickBooksBuilder = services.AddOptions<QuickBooksOptions>()
            .Bind(configuration.GetSection("QuickBooks"));
        if (validateOnStart) quickBooksBuilder.ValidateOnStart();

        // JWT settings for auth token validation
        var jwtBuilder = services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Key), "Jwt:Key is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.");
        if (validateOnStart) jwtBuilder.ValidateOnStart();

        // Queueing settings (Service Bus may be intentionally empty and handled via NoOpQueuePublisher)
        var serviceBusBuilder = services.AddOptions<ServiceBusOptions>()
            .Bind(configuration.GetSection("ServiceBus"));
        if (validateOnStart) serviceBusBuilder.ValidateOnStart();

        // API rate limiter settings
        var rateLimiterBuilder = services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection("RateLimiting"))
            .Validate(o => o.PermitLimit > 0, "RateLimiting:PermitLimit must be > 0.")
            .Validate(o => o.WindowSeconds > 0, "RateLimiting:WindowSeconds must be > 0.");
        if (validateOnStart) rateLimiterBuilder.ValidateOnStart();

        // Optional Azure OpenAI settings (only used by the CFO assistant when endpoint/key exist)
        var azureOpenAiBuilder = services.AddOptions<AzureOpenAiOptions>()
            .Bind(configuration.GetSection("AzureOpenAI"));
        if (validateOnStart) azureOpenAiBuilder.ValidateOnStart();

        // Maps ConnectionStrings:DefaultConnection → DefaultConnection; optional CommandTimeoutSeconds under same section if present
        var databaseOptionsBuilder = services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection("ConnectionStrings"));

        if (validateOnStart)
        {
            databaseOptionsBuilder
                .Validate(
                    o => !string.IsNullOrWhiteSpace(o.DefaultConnection),
                    "ConnectionStrings:DefaultConnection is required.")
                .ValidateOnStart();
        }

        return services;
    }
}

