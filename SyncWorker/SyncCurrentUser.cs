using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker
{
    /// <summary>
    /// ICurrentUser implementation for background worker context.
    /// Values are set from the queue message before resolving scoped services.
    /// </summary>
    public class SyncCurrentUser : ICurrentUser
    {
        public string? UserId { get; set; }
        public string? RealmId { get; set; }
    }
}
