using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Infrastructure.Queue;

namespace QuickBooksAPI.Services
{
    public class SyncService : ISyncService
    {
        private readonly IQueuePublisher _publisher;
        private readonly ISyncStatusRepository _statusRepo;

        public SyncService(
            IQueuePublisher publisher,
            ISyncStatusRepository statusRepo)
        {
            _publisher = publisher;
            _statusRepo = statusRepo;
        }

        public async Task StartFullSyncAsync(string companyId, string userId)
        {
            if (await _statusRepo.IsRunningAsync(companyId))
                throw new InvalidOperationException("Sync already running for this company.");

            await _statusRepo.SetStatusAsync(companyId, "Queued");

            var msg = new FullSyncMessage
            {
                CompanyId = companyId,
                UserId = userId,
                RequestedAt = DateTime.UtcNow
            };

            await _publisher.PublishAsync(msg);
        }

        public async Task<SyncStatusDto?> GetSyncStatusAsync(string companyId)
        {
            return await _statusRepo.GetStatusAsync(companyId);
        }
    }
}
