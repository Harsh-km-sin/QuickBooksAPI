using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using QuickBooksAPI.Middleware;

namespace QuickBooksAPI.Infrastructure;

/// <summary>
/// API middleware pipeline (Swagger in Development, correlation, rate limit, CORS, auth). Health endpoints: <c>/health</c> and <c>/health/ready</c> (readiness), <c>/health/live</c> (liveness).
/// </summary>
public static class QuickBooksApiWebApplicationExtensions
{
    public static WebApplication UseQuickBooksApiPipeline(this WebApplication app)
    {
        var allowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        if (app.Environment.IsDevelopment() || string.Equals(
                app.Environment.EnvironmentName,
                "OpenApiExport",
                StringComparison.OrdinalIgnoreCase))
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = string.Empty;
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

        var readyOptions = new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        };
        var liveOptions = new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        };

        app.MapHealthChecks("/health/live", liveOptions);
        app.MapHealthChecks("/health/ready", readyOptions);
        app.MapHealthChecks("/health", readyOptions);

        return app;
    }
}
