using System;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public interface IQuickBooksBillService
    {
        /// <summary>
        /// Query Bills from QuickBooks (for sync). Returns raw JSON query response.
        /// </summary>
        Task<string> GetBillsAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null);

        Task<string> CreateBillAsync(string accessToken, string realmId, string billPayload);
        Task<string> UpdateBillAsync(string accessToken, string realmId, string billPayload);
        Task<string> DeleteBillAsync(string accessToken, string realmId, string billPayload);
    }
}
