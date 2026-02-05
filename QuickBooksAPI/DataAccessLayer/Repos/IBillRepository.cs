using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IBillRepository
    {
        IDbConnection CreateOpenConnection();
        Task UpsertBillsAsync(IEnumerable<QBOBillHeader> headers, IEnumerable<BillLineUpsertRow> lines, IDbConnection connection, IDbTransaction tx);
        /// <summary>Soft-deletes a bill in the DB by setting IsDeleted = 1. Returns true if a row was updated.</summary>
        Task<bool> SoftDeleteBillAsync(string realmId, string qboBillId);
    }
}
