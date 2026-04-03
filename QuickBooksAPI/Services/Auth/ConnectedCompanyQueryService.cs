using Microsoft.Extensions.Logging;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services.Auth;

public class ConnectedCompanyQueryService : IConnectedCompanyQueryService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<ConnectedCompanyQueryService> _logger;

    public ConnectedCompanyQueryService(ICompanyRepository companyRepository, ILogger<ConnectedCompanyQueryService> logger)
    {
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ConnectedCompanyDto>>> GetConnectedCompaniesAsync(int userId)
    {
        try
        {
            var companies = await _companyRepository.GetConnectedCompaniesByUserIdAsync(userId);
            var result = companies.Select(c => new ConnectedCompanyDto
            {
                Id = c.Id,
                QboRealmId = c.QboRealmId,
                CompanyName = c.CompanyName,
                ConnectedAtUtc = c.ConnectedAtUtc,
                IsQboConnected = c.IsQboConnected
            });

            return ApiResponse<IEnumerable<ConnectedCompanyDto>>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch connected companies for UserId={UserId}", userId);
            return ApiResponse<IEnumerable<ConnectedCompanyDto>>.Fail("Failed to fetch connected companies.", new[] { ex.Message });
        }
    }
}
