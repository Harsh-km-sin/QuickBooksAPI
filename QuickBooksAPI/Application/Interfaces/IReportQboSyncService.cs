using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IReportQboSyncService
    {
        /// <summary>
        /// Pulls P&amp;L and Balance Sheet history from QBO and stores it.
        /// </summary>
        /// <param name="force">
        /// When false (the Full Sync path) the pull is skipped if no financial entity has changed
        /// since the last report sync. When true (the manual "Generate Reports" endpoint) the pull
        /// always runs. A change to the company's accounting basis or fiscal-year start forces a
        /// full re-pull regardless, since it invalidates every stored period.
        /// </param>
        /// <returns>The number of report periods stored.</returns>
        Task<ApiResponse<int>> SyncFromQuickBooksAsync(int userId, string realmId, bool force = false);
    }
}
