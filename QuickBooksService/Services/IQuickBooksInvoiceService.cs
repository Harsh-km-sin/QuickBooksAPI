using System;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public interface IQuickBooksInvoiceService
    {
        Task<string> GetInvoiceAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null);
        Task<string> CreateInvoiceAsync(string accessToken, string realmId, string invoicePayload);
        Task<string> UpdateInvoiceAsync(string accessToken, string realmId, string invoicePayload);
        Task<string> DeleteInvoiceAsync(string accessToken, string realmId, string invoicePayload);
        Task<string> VoidInvoiceAsync(string accessToken, string realmId, string invoicePayload);
    }
}
