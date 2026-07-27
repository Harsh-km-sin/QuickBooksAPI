using System;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public interface IQuickBooksTermService
    {
        Task<string> GetTermsAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 1000);
    }
}
