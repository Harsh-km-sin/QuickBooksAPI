namespace QuickBooksAPI.Middleware
{
    public class CorrelationIdMiddleware
    {
        public const string CorrelationIdHeader = "X-Correlation-Id";
        public const string CorrelationIdItemKey = "CorrelationId";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                ?? Guid.NewGuid().ToString("N");
            context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            context.Items[CorrelationIdItemKey] = correlationId;
            await _next(context);
        }
    }
}
