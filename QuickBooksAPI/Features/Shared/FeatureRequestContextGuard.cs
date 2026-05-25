using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Features.Shared;

/// <summary>
/// Shared user/realm validation for feature HTTP handlers.
/// </summary>
internal static class FeatureRequestContextGuard
{
    public const string MissingContextMessage = "User context is missing. Please sign in and connect QuickBooks.";

    public static bool TryGetUserRealm(
        IRequestContext requestContext,
        out int userId,
        out string realmId,
        out string? error)
    {
        userId = 0;
        realmId = string.Empty;
        error = null;

        if (string.IsNullOrEmpty(requestContext.UserId) || string.IsNullOrEmpty(requestContext.RealmId))
        {
            error = MissingContextMessage;
            return false;
        }

        userId = int.Parse(requestContext.UserId);
        realmId = requestContext.RealmId;
        return true;
    }

    /// <summary>Validates signed-in user + realm (realm only needed for read operations that still require auth).</summary>
    public static bool TryGetRealm(
        IRequestContext requestContext,
        out string realmId,
        out string? error)
    {
        realmId = string.Empty;
        error = null;
        if (string.IsNullOrEmpty(requestContext.UserId) || string.IsNullOrEmpty(requestContext.RealmId))
        {
            error = MissingContextMessage;
            return false;
        }

        realmId = requestContext.RealmId;
        return true;
    }
}
