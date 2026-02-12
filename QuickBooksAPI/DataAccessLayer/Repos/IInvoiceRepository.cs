using QuickBooksAPI.DataAccessLayer.Models;
using System.Data;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IInvoiceRepository
    {
        IDbConnection CreateOpenConnection();
        Task<IEnumerable<QBOInvoiceHeader>> GetAllByRealmAsync(string realmId);
        Task UpsertInvoicesAsync(IEnumerable<QBOInvoiceHeader> headers, IEnumerable<InvoiceLineUpsertRow> lines, IDbConnection connection, IDbTransaction tx);
        [Obsolete("Use UpsertInvoicesAsync with SP instead.")]
        Task<int> UpsertInvoiceHeadersAsync(IEnumerable<QBOInvoiceHeader> invoices, IDbConnection connection, IDbTransaction tx);
        Task DeleteInvoiceLinesAsync(long invoiceId, IDbConnection connection, IDbTransaction tx);
        Task<int> InsertInvoiceLinesAsync(IEnumerable<QBOInvoiceLine> lines, IDbConnection connection, IDbTransaction tx);
        Task<long> GetInvoiceIdAsync(string qbInvoiceId, string realmId, IDbConnection connection, IDbTransaction tx);
    }
}
