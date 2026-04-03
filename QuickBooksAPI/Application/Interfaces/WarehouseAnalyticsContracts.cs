using QuickBooksAPI.API.DTOs.Response;

namespace QuickBooksAPI.Application.Interfaces;

public record CashRunwayResult(
    decimal CurrentCash,
    decimal MonthlyBurn,
    decimal ExpectedRevenue,
    decimal RunwayMonths);

public interface ICashRunwayService
{
    Task<CashRunwayResult> GetRunwayAsync(int userId, string realmId, CancellationToken cancellationToken = default);
}

public interface IVendorAnalyticsService
{
    Task<IReadOnlyList<VendorSpendDto>> GetTopVendorsAsync(int userId, string realmId, int periodDays, int limit, CancellationToken cancellationToken = default);
    Task<VendorSpendSummaryDto> GetSummaryAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public interface ICustomerProfitabilityService
{
    Task<IReadOnlyList<CustomerProfitabilityDto>> GetCustomerProfitabilityAsync(int userId, string realmId, DateTime from, DateTime to, int top = 50, CancellationToken cancellationToken = default);
}

public interface IRevenueExpensesService
{
    Task<IReadOnlyList<RevenueExpensesMonthlyDto>> GetMonthlyAsync(int userId, string realmId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public interface IKpiService
{
    Task<IReadOnlyList<KpiSnapshotDto>> GetKpisAsync(int userId, string realmId, DateTime from, DateTime to, IReadOnlyList<string>? names, CancellationToken cancellationToken = default);
}

public interface IFinancialWarehouseService
{
    Task RebuildForCompanyAsync(string realmId, string userId, CancellationToken cancellationToken = default);
}

public interface IAnomalyDetectionService
{
    Task DetectAsync(int userId, string realmId, CancellationToken cancellationToken = default);
}
