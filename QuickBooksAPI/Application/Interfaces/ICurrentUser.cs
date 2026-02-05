namespace QuickBooksAPI.Application.Interfaces
{
    public interface ICurrentUser
    {
        string? UserId { get; }
        string? RealmId { get; }
    }
}
