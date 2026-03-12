using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services
{
    public interface IFinancialWarehouseService
    {
        Task RebuildForCompanyAsync(string realmId, string userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Application service that coordinates rebuilding the financial warehouse
    /// for a given company and user. Intended to be invoked from background
    /// workers (SyncWorker) after a full sync completes.
    /// </summary>
    public class FinancialWarehouseService : IFinancialWarehouseService
    {
        private readonly IFinancialWarehouseRepository _repository;

        public FinancialWarehouseService(IFinancialWarehouseRepository repository)
        {
            _repository = repository;
        }

        public async Task RebuildForCompanyAsync(string realmId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(userId))
                return;

            if (!int.TryParse(userId, out var parsedUserId))
                return;

            await _repository.RebuildFactsAsync(parsedUserId, realmId, cancellationToken);
        }
    }
}

