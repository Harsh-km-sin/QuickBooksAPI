using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.DataAccessLayer.Repos
{
    public interface IVendorRepository
    {
        public Task<int> UpsertVendorsAsync(IEnumerable<Vendor> vendors, int userId, string realmId);

        public Task<DateTime?> GetLastUpdatedTimeAsync(int userId, string realmId);

        /// <summary>Soft-deletes a vendor by setting DeletedAt and DeletedBy. Returns true if a row was updated.</summary>
        public Task<bool> SoftDeleteAsync(int userId, string realmId, string qboId);
    }
}
