using System;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public interface IQuickBooksVendorService
    {
        Task<string> GetVendorsAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null);
        Task<string> CreateVendorAsync(string accessToken, string realmId, string vendorPayload);
        Task<string> UpdateVendorAsync(string accessToken, string realmId, string vendorPayload);
        Task<string> SoftDeleteVendorAsync(string accessToken, string realmId, string id, string syncToken);
    }
}
