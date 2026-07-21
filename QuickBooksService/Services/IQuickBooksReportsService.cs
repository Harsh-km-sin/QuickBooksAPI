using System;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public interface IQuickBooksReportsService
    {
        Task<string> GetProfitAndLossAsync(
            string accessToken,
            string realmId,
            DateTime startDate,
            DateTime endDate,
            string summarizeColumnBy = "Month",
            string accountingMethod = "Accrual");

        Task<string> GetBalanceSheetAsync(
            string accessToken,
            string realmId,
            DateTime startDate,
            DateTime endDate,
            string summarizeColumnBy = "Month",
            string accountingMethod = "Accrual");
    }
}
