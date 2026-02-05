using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Infrastructure.Identity
{
    public class CurrentUser : ICurrentUser
    {
        public string? UserId { get; internal set; }
        public string? RealmId { get; internal set; }
    }

}
