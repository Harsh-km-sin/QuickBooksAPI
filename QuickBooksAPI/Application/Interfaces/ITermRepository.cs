using System.Collections.Generic;
using System.Threading.Tasks;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface ITermRepository
    {
        Task<IEnumerable<QBOTerm>> GetAllByRealmAsync(string realmId, bool activeOnly = true);
        Task<QBOTerm?> GetByQbIdAsync(string qboTermId, string realmId);
        Task UpsertTermsAsync(IEnumerable<QBOTerm> terms);
    }
}
