using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces
{
    public interface IQboCompanyMetadataService
    {
        /// <summary>
        /// Refreshes the QBO metadata that report sync depends on (accounting basis, company start
        /// date, fiscal-year start month) and persists it when it has changed.
        ///
        /// Runs on every sync, not just at OAuth connect, for two reasons: companies connected
        /// before this feature existed have no metadata at all and would otherwise never get any,
        /// and a company that changes its reporting basis in QBO must be detected so stored reports
        /// can be invalidated.
        ///
        /// Best-effort: on failure the company is returned unchanged rather than throwing, so a
        /// metadata hiccup never fails a sync.
        /// </summary>
        /// <returns>The company with current metadata applied.</returns>
        Task<Company> EnsureMetadataAsync(int userId, string realmId, string accessToken, Company company);
    }
}
