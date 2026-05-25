using Moq;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Application.Sync;
using QuickBooksAPI.Features.Products.Handlers;
using QuickBooksAPI.Integrations.Abstractions;

namespace QuickBooksAPI.UnitTests.Products;

public sealed class ProductServicesSyncTests
{
    [Fact]
    public async Task GetProductsAsync_UsesGateway_FirstSyncEmptyResult_UpdatesWatermark()
    {
        var ctx = new Mock<IRequestContext>();
        ctx.Setup(c => c.UserId).Returns("1");
        ctx.Setup(c => c.RealmId).Returns("realm");

        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.RefreshTokenIfExpiredAsync(1, "realm"))
            .ReturnsAsync(new QboAccessTokenSnapshot { AccessToken = "tok" });

        var gateway = new Mock<IProductAccountingSyncGateway>();
        gateway
            .Setup(g => g.FetchProductsPageAsync("tok", "realm", 1, 1000, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"QueryResponse":{}}""");

        var qboSync = new Mock<IQboSyncStateRepository>();
        qboSync.Setup(s => s.GetLastUpdatedAfterAsync(1, "realm", QboSyncEntityType.Products))
            .ReturnsAsync((DateTime?)null);
        qboSync.Setup(s => s.UpdateLastUpdatedAfterAsync(1, "realm", QboSyncEntityType.Products, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        var repo = new Mock<IProductRepository>();

        var sut = new SyncProductsHandler(
            ctx.Object,
            auth.Object,
            gateway.Object,
            repo.Object,
            qboSync.Object);

        var result = await sut.HandleAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.Data);
        gateway.Verify(
            g => g.FetchProductsPageAsync("tok", "realm", 1, 1000, null, It.IsAny<CancellationToken>()),
            Times.Once);
        qboSync.Verify(
            s => s.UpdateLastUpdatedAfterAsync(1, "realm", QboSyncEntityType.Products, It.IsAny<DateTime>()),
            Times.Once);
    }
}
