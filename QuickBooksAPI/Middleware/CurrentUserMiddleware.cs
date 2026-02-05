using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.Identity;
using System.Security.Claims;
using System.Text.Json;

namespace QuickBooksAPI.Middleware
{
    public class CurrentUserMiddleware
    {
        private readonly RequestDelegate _next;

        // Endpoints that don't require UserId/RealmId validation
        private static readonly HashSet<string> BypassPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/login",
            "/api/auth/SignUp",
            "/api/auth/callback",
            "/swagger",
            "/",
        };

        public CurrentUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, CurrentUser currentUser)
        {
            // Check if this is an authenticated request
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Extract claims
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // RealmId extraction logic:
                // 1. Check for X-Realm-Id header (useful for multi-company selection)
                var realmId = context.Request.Headers["X-Realm-Id"].FirstOrDefault();

                // 2. Check for singular realm_id claim
                if (string.IsNullOrWhiteSpace(realmId))
                {
                    realmId = context.User.FindFirst("realm_id")?.Value;
                }

                // 3. Fallback to plural RealmIds claim (deserializing JSON array like ["567..."])
                if (string.IsNullOrWhiteSpace(realmId))
                {
                    var realmIdsJson = context.User.FindFirst("RealmIds")?.Value;
                    if (!string.IsNullOrWhiteSpace(realmIdsJson))
                    {
                        try
                        {
                            var realmIds = JsonSerializer.Deserialize<List<string>>(realmIdsJson);
                            realmId = realmIds?.FirstOrDefault();
                        }
                        catch { /* Silently fail if JSON is malformed */ }
                    }
                }

                // Check if the current path should bypass validation
                var path = context.Request.Path.Value ?? string.Empty;
                var shouldBypass = BypassPaths.Any(bp => path.StartsWith(bp, StringComparison.OrdinalIgnoreCase));

                // If not bypassing, validate required claims
                if (!shouldBypass)
                {
                    var missingClaims = new List<string>();
                    if (string.IsNullOrWhiteSpace(userId)) missingClaims.Add("User ID");
                    if (string.IsNullOrWhiteSpace(realmId)) missingClaims.Add("Realm ID");

                    if (missingClaims.Count > 0)
                    {
                        await RejectRequest(context, string.Join(", ", missingClaims) + " is missing from token");
                        return;
                    }
                }
                // Set the current user context
                currentUser.UserId = userId;
                currentUser.RealmId = realmId;
            }

            await _next(context);
        }

        private static async Task RejectRequest(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var payload = ApiResponse<object>.Fail(
                "Forbidden",
                new[] { message }
            );

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
