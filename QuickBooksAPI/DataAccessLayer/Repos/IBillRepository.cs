using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.DTOs;
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
        Task<IEnumerable<QBOBillHeader>> GetAllByRealmAsync(string realmId);
        Task<PagedResult<QBOBillHeader>> GetPagedByRealmAsync(string realmId, int page, int pageSize, string? search);
        /// <summary>Gets a single bill by QuickBooks bill id and realm. Returns null if not found or deleted.</summary>
        Task<QBOBillHeader?> GetByQboBillIdAsync(string realmId, string qboBillId);
    }
}
