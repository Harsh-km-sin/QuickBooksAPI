namespace QuickBooksAPI.Services.CfoAssistant;

internal static class CfoAssistantIntentMatching
{
    public static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
}
