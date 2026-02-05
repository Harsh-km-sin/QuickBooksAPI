namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class QuickBooksToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = null!;
        public string IdToken { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string TokenType { get; set; } = null!;
        public int ExpiresIn { get; set; }
        public int XRefreshTokenExpiresIn { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
