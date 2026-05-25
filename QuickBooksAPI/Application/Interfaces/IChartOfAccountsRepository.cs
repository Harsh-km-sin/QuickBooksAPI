using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Dtos;

namespace QuickBooksAPI.Application.Interfaces;

public interface IChartOfAccountsRepository
{
    Task<int> UpsertChartOfAccountsAsync(IEnumerable<ChartOfAccountsUpsertDto> accounts);
    Task<IEnumerable<ChartOfAccountsItemDto>> GetAllByUserAndRealmAsync(int userId, string realmId);
    Task<PagedResult<ChartOfAccountsItemDto>> GetPagedByUserAndRealmAsync(int userId, string realmId, int page, int pageSize, string? search);
}
