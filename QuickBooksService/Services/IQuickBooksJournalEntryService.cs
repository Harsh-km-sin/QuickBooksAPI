using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickBooksService.Services
{
    public interface IQuickBooksJournalEntryService
    {
        public Task<string> GetJournalEntryAsync(string accessToken, string realmId, int startPosition = 1, int maxResults = 100, DateTime? lastUpdatedAfter = null);
    }
}
