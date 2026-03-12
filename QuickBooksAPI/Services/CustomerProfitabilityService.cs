using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace QuickBooksAPI.Services
{
    public interface ICustomerProfitabilityService
    {
        Task<IReadOnlyList<CustomerProfitabilityDto>> GetCustomerProfitabilityAsync(int userId, string realmId, DateTime from, DateTime to, int top = 50, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Exposes customer profitability from the financial warehouse for CFO analytics.
    /// </summary>
    public class CustomerProfitabilityService : ICustomerProfitabilityService
    {
        private readonly IFinancialWarehouseRepository _warehouse;

        public CustomerProfitabilityService(IFinancialWarehouseRepository warehouse)
        {
            _warehouse = warehouse;
        }

        public async Task<IReadOnlyList<CustomerProfitabilityDto>> GetCustomerProfitabilityAsync(int userId, string realmId, DateTime from, DateTime to, int top = 50, CancellationToken cancellationToken = default)
        {
            var rows = await _warehouse.GetCustomerProfitabilityAsync(userId, realmId, from, to, top, cancellationToken);
            return rows.Select(r => new CustomerProfitabilityDto
            {
                CustomerName = r.CustomerName,
                Revenue = r.Revenue,
                CostOfGoods = r.CostOfGoods,
                GrossMargin = r.GrossMargin,
                MarginPct = decimal.Round(r.MarginPct, 2),
                PeriodStart = from.Date
            }).ToList();
        }
    }
}
