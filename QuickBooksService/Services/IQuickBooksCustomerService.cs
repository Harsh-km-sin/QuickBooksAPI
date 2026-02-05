using System;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public interface IQuickBooksCustomerService
    {
        public Task<string> GetCustomersAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null);
        public Task<string> CreateCustomerAsync(string accessToken, string realmId, string customerPayload);
        public Task<string> UpdateCustomerAsync(string accessToken, string realmId, string customerPayload);
        public Task<string> DeleteCustomerAsync(string accessToken, string realmId, string customerPayload);
    }
}
