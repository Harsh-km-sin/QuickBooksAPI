using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure;
using QuickBooksAPI.Infrastructure.Identity;
using QuickBooksAPI.Middleware;
using QuickBooksAPI.Services;
using QuickBooksService.Services;
using QuickBooksShared.Options;
using QuickBooksShared;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<RequestContext>();
builder.Services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());
builder.Services.AddQuickBooksAuthAndEntityApplicationServices();
builder.Services.AddQuickBooksAnalyticsApplicationServices();

// Typed options (Phase 1) - shared binding to prevent host drift
builder.Services.AddQuickBooksTypedOptions(builder.Configuration, validateOnStart: true);

builder.Services.AddQuickBooksServiceBusAndSync(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = new JwtOptions();
        builder.Configuration.GetSection("Jwt").Bind(jwtOptions);

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

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// Default HttpClient; for retry/circuit breaker use a named client with AddHttpClient("QuickBooks").AddPolicyHandler(...)
builder.Services.AddHttpClient();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "ready" });

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddCors(options =>
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
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); // fallback; set Cors:AllowedOrigins in production
    });
});

var rateLimitingOptions = new RateLimitingOptions();
builder.Configuration.GetSection("RateLimiting").Bind(rateLimitingOptions);

builder.Services.AddRateLimiter(options =>
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

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "QuickBooksAPI", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = string.Empty; // Swagger UI now available at "/"
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickBooksAPI v1");
    });
}


app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseRateLimiter();
app.UseCors(allowedOrigins.Length > 0 ? "Default" : "AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
