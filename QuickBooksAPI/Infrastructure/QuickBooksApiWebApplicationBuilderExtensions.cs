using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Infrastructure.Identity;
using QuickBooksShared.Options;

namespace QuickBooksAPI.Infrastructure;

/// <summary>
/// API host composition (JWT, Swagger, CORS, rate limiting, health). See also <see cref="QuickBooksApiWebApplicationExtensions"/>.
/// </summary>
public static class QuickBooksApiWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddQuickBooksApiHostServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddQuickBooksAuthentication(builder.Configuration);
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = options.DefaultPolicy;
        });

        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddQuickBooksApiHealthChecks(builder.Configuration);

        builder.Services.AddQuickBooksCorsAndRateLimiting(builder.Configuration, builder.Environment);

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "QuickBooksAPI", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer {token}'"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return builder;
    }

    private static IServiceCollection AddQuickBooksAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOptions = new JwtOptions();
                configuration.GetSection("Jwt").Bind(jwtOptions);

                if (string.IsNullOrWhiteSpace(jwtOptions.Key))
                    throw new InvalidOperationException("Jwt:Key configuration is missing or empty.");
                if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
                    throw new InvalidOperationException("Jwt:Issuer configuration is missing or empty.");
                if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
                    throw new InvalidOperationException("Jwt:Audience configuration is missing or empty.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key)
                    ),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        if (context.Response.HasStarted) return;

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var payload = ApiResponse<object>.Fail("Unauthorized", new[] { "Authentication required" });
                        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                    },

                    OnForbidden = async context =>
                    {
                        if (context.Response.HasStarted) return;

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var payload = ApiResponse<object>.Fail("Forbidden", new[] { "You are not allowed to access this resource" });
                        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                    },

                    OnAuthenticationFailed = async context =>
                    {
                        if (context.Response.HasStarted) return;

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var payload = ApiResponse<object>.Fail("Invalid token", new[] { "Authentication failed. Please sign in again." });
                        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                    }
                };
            });

        return services;
    }

    private static IServiceCollection AddQuickBooksCorsAndRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var isDevelopment = environment.IsDevelopment();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
            options.AddPolicy("Default", policy =>
            {
                if (allowedOrigins.Length > 0)
                    policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                else if (isDevelopment)
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                else
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });

        var rateLimitingOptions = new RateLimitingOptions();
        configuration.GetSection("RateLimiting").Bind(rateLimitingOptions);

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var permitLimit = rateLimitingOptions.PermitLimit;
                var windowSeconds = rateLimitingOptions.WindowSeconds;
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.User.Identity?.IsAuthenticated == true ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous" : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromSeconds(windowSeconds) });
            });
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                var payload = ApiResponse<object>.Fail("Too Many Requests", new[] { "Rate limit exceeded. Please try again later." });
                await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(payload));
            };
        });

        return services;
    }
}
