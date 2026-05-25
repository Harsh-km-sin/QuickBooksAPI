namespace QuickBooksAPI.Application.Dtos;

/// <summary>
/// QBO access token (and optional fields) for API calls, without coupling callers to persistence models.
/// </summary>
public sealed class QboAccessTokenSnapshot
{
    public string AccessToken { get; set; } = string.Empty;
}
