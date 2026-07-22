using System.Data;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.DataAccessLayer.DTOs;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IReportRepository
    {
        /// <summary>Opens a connection the caller owns, so a whole report pull commits atomically.</summary>
        IDbConnection CreateOpenConnection();

        /// <summary>Upserts one report period (header + columns + rows + values) via <c>dbo.UpsertReportRun</c>.</summary>
        Task UpsertReportRunAsync(FlattenedReport report, IDbConnection connection, IDbTransaction transaction);

        /// <summary>
        /// Removes every stored run for a report type. Used when the company's accounting basis or
        /// fiscal-year start changes, which invalidates all stored periods and would otherwise leave
        /// old runs overlapping the new ones.
        /// </summary>
        Task<int> DeleteAllRunsAsync(int userId, string realmId, string reportType);

        /// <summary>Aggregated P&amp;L lines for a range, already summed by <c>dbo.GetProfitAndLossTree</c>.</summary>
        Task<IEnumerable<ReportLineRow>> GetProfitAndLossAsync(
            int userId, string realmId, DateTime rangeStart, DateTime rangeEnd, string accountingMethod);

        /// <summary>Balance Sheet lines as of a date, from <c>dbo.GetBalanceSheetTree</c>. Never summed.</summary>
        Task<IEnumerable<ReportLineRow>> GetBalanceSheetAsync(
            int userId, string realmId, DateTime rangeStart, DateTime rangeEnd, string accountingMethod);

        /// <summary>Which periods are resident, so the UI can bound its date picker.</summary>
        Task<IEnumerable<QBOReportRun>> GetSyncedRunsAsync(int userId, string realmId, string reportType);

        /// <summary>Most recent successful pull for a report type, used by the skip-if-unchanged check.</summary>
        Task<DateTimeOffset?> GetLastSyncedAtAsync(int userId, string realmId, string reportType);
    }
}
