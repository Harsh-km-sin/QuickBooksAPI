using System;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public interface IQuickBooksProductService
    {
        public Task<string> GetProductsAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null);
        public Task<string> CreateProductAsync(string accessToken, string realmId, string productPayload);
        public Task<string> UpdateProductAsync(string accessToken, string realmId, string productPayload);
        public Task<string> DeleteProductAsync(string accessToken, string realmId, string productPayload);
    }
}
