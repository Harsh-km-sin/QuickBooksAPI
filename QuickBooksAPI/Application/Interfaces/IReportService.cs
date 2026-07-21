using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces
{
    /// <summary>HTTP-facing façade for the Reports feature. Mirrors the other entity service façades.</summary>
    public interface IReportService
    {
        Task<ApiResponse<ReportTreeDto>> GetProfitAndLossAsync(DateTime? startDate, DateTime? endDate, bool useFiscalYear);
        Task<ApiResponse<ReportTreeDto>> GetBalanceSheetAsync(DateTime? asOfDate);
        Task<ApiResponse<IEnumerable<ReportPeriodDto>>> GetSyncedPeriodsAsync(string reportType);

        /// <summary>Manual re-pull, used by the "Generate Reports" action. Always forces a pull.</summary>
        Task<ApiResponse<int>> SyncReportsAsync();

        /// <summary>Full Sync path: honours the skip-if-nothing-changed check.</summary>
        Task<ApiResponse<int>> SyncReportsForFullSyncAsync();
    }
}
