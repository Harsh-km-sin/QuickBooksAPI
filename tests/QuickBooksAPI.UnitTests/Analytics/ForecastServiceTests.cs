using Moq;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Features.Forecast;

namespace QuickBooksAPI.UnitTests.Analytics;

public class ForecastServiceTests
{
    [Fact]
    public async Task GetForecastAsync_WhenScenarioMissing_ReturnsNull()
    {
        var scenarioRepo = new Mock<IForecastScenarioRepository>();
        scenarioRepo
            .Setup(r => r.GetByIdAndUserRealmAsync(99, 1, "r1", default))
            .ReturnsAsync((ForecastScenario?)null);

        var sut = new ForecastService(
            scenarioRepo.Object,
            Mock.Of<IForecastResultRepository>(),
            Mock.Of<IFinancialWarehouseRepository>(),
            Mock.Of<ICashRunwayService>());

        var result = await sut.GetForecastAsync(99, 1, "r1");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAndComputeAsync_PersistsResultsAndMarksCompleted()
    {
        const int scenarioId = 42;
        var scenarioRepo = new Mock<IForecastScenarioRepository>();
        scenarioRepo
            .Setup(r => r.InsertAsync(It.IsAny<ForecastScenario>(), default))
            .ReturnsAsync(scenarioId);

        var resultRepo = new Mock<IForecastResultRepository>();
        var warehouse = new Mock<IFinancialWarehouseRepository>();
        var month = new DateTime(2025, 1, 1);
        warehouse
            .Setup(w => w.GetRevenueExpensesMonthlyAsync(1, "realm", It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<RevenueExpensesMonthlyRow>
            {
                new() { MonthStart = month, Revenue = 1000m, Expenses = 400m },
                new() { MonthStart = month.AddMonths(1), Revenue = 1100m, Expenses = 420m },
                new() { MonthStart = month.AddMonths(2), Revenue = 1200m, Expenses = 440m }
            });

        var runway = new Mock<ICashRunwayService>();
        runway
            .Setup(s => s.GetRunwayAsync(1, "realm", default))
            .ReturnsAsync(new CashRunwayResult(5000m, 400m, 3m, 0m));

        var sut = new ForecastService(
            scenarioRepo.Object,
            resultRepo.Object,
            warehouse.Object,
            runway.Object);

        var id = await sut.CreateAndComputeAsync(1, "realm", "Q1", 2, null, "tester");

        Assert.Equal(scenarioId, id);
        resultRepo.Verify(
            r => r.InsertBatchAsync(It.Is<List<ForecastResult>>(list => list.Count == 2), default),
            Times.Once);
        scenarioRepo.Verify(r => r.UpdateStatusAsync(scenarioId, "Completed", default), Times.Once);
    }
}
