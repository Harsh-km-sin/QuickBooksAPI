using QuickBooksAPI.API.DTOs.Response;
using System.Net;
using System.Text.Json;

namespace QuickBooksAPI.Middleware
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;
        private const string CorrelationIdHeader = "X-Correlation-Id";

        public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = context.Items["CorrelationId"]?.ToString()
                    ?? context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                    ?? Guid.NewGuid().ToString("N");
                _logger.LogError(ex, "Unhandled exception. CorrelationId={CorrelationId}", correlationId);

                // Once the response has started, headers/status/body are read-only; do not try to modify them.
                if (!context.Response.HasStarted)
                {
                    context.Response.Headers.Append(CorrelationIdHeader, correlationId);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var payload = ApiResponse<object>.Fail("An error occurred. Please try again later.", new[] { $"CorrelationId: {correlationId}" });
                    await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
