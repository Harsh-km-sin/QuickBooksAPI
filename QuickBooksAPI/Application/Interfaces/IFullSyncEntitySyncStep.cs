namespace QuickBooksAPI.Application.Interfaces;

/// <summary>
/// One QBO entity pull during a full sync (runs inside a scoped <see cref="IServiceProvider"/>).
/// </summary>
public interface IFullSyncEntitySyncStep
{
    string EntityName { get; }

    string EntityTypeForSyncState { get; }

    Task<int> ExecuteAsync(IServiceProvider scopedServices, CancellationToken cancellationToken);
}
