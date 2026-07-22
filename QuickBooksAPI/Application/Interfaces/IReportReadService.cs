using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IReportReadService
    {
        /// <summary>P&amp;L for a month-aligned range, summed from stored monthly columns.</summary>
        Task<ApiResponse<ReportTreeDto>> GetProfitAndLossAsync(
            int userId, string realmId, DateTime startDate, DateTime endDate);

        /// <summary>Balance Sheet as of a date — the latest stored month-end at or before it.</summary>
        Task<ApiResponse<ReportTreeDto>> GetBalanceSheetAsync(
            int userId, string realmId, DateTime asOfDate);

        /// <summary>P&amp;L for the fiscal year containing <paramref name="anyDateInYear"/>.</summary>
        Task<ApiResponse<ReportTreeDto>> GetProfitAndLossForFiscalYearAsync(
            int userId, string realmId, DateTime anyDateInYear);

        /// <summary>Which periods are resident, so the UI can bound its date picker.</summary>
        Task<ApiResponse<IEnumerable<ReportPeriodDto>>> GetSyncedPeriodsAsync(
            int userId, string realmId, string reportType);
    }
}
