namespace QuickBooksShared.Options;

public class QuickBooksOptions
{
    public string AuthUrl { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = string.Empty;
    public string RevokeUrl { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;

    public string RequestURL { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string FrontendBaseUrl { get; set; } = string.Empty;

    // Used only for environment-based validation decisions.
    public string Environment { get; set; } = string.Empty;
}

