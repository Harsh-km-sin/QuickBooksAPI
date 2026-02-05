using System.Text.Json.Serialization;

namespace QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs
{
    public class TokenResponseDto
    {
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 0;

        [JsonPropertyName("x_refresh_token_expires_in")]
        public int XRefreshTokenExpiresIn { get; set; } = 0;
    }
}
