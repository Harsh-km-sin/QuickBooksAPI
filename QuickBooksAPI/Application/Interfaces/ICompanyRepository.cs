using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface ICompanyRepository
{
    Task<Company?> GetByUserAndRealmAsync(int userId, string realmId);
    Task<IEnumerable<Company>> GetConnectedCompaniesByUserIdAsync(int userId);
    Task<IEnumerable<(int UserId, string RealmId)>> GetDistinctConnectedUserRealmAsync();
    Task UpsertCompanyAsync(Company company);

    /// <summary>
    /// Updates only the QBO metadata columns used by report sync. Deliberately separate from
    /// <see cref="UpsertCompanyAsync"/>, whose MERGE overwrites the token columns unconditionally —
    /// a metadata refresh must never touch credentials.
    /// </summary>
    Task UpdateCompanyMetadataAsync(
        int userId,
        string realmId,
        string? accountingBasis,
        DateTime? companyStartDate,
        int? fiscalYearStartMonth);
    Task ClearCompanyTokenAsync(int userId, string realmId);
}
