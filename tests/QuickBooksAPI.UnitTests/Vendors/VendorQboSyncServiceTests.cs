using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksAPI.Services.Vendors;

namespace QuickBooksAPI.UnitTests.Vendors;

public sealed class VendorQboSyncServiceTests
{
    [Fact]
    public async Task SyncFromQuickBooksAsync_UsesGateway_AndSucceedsWithEmptyPage()
    {
        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.RefreshTokenIfExpiredAsync(1, "realm"))
            .ReturnsAsync(new QboAccessTokenSnapshot { AccessToken = "tok" });

        var gateway = new Mock<IVendorAccountingSyncGateway>();
        gateway.Setup(g => g.FetchVendorsPageAsync("tok", "realm", 1, 1000, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"QueryResponse":{}}""");

        var vendors = new Mock<IVendorRepository>();
        var syncState = new Mock<IQboSyncStateRepository>();
        syncState.Setup(s => s.GetLastUpdatedAfterAsync(1, "realm", QboEntityType.Vendors.ToString()))
            .ReturnsAsync((DateTime?)null);
        syncState.Setup(s => s.UpdateLastUpdatedAfterAsync(1, "realm", QboEntityType.Vendors.ToString(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        var sut = new VendorQboSyncService(
            auth.Object,
            gateway.Object,
            vendors.Object,
            syncState.Object,
            NullLogger<VendorQboSyncService>.Instance);

        var result = await sut.SyncFromQuickBooksAsync(1, "realm");

        Assert.True(result.Success);
        Assert.Equal(0, result.Data);
        gateway.Verify(
            g => g.FetchVendorsPageAsync("tok", "realm", 1, 1000, null, It.IsAny<CancellationToken>()),
            Times.Once);
        syncState.Verify(
            s => s.UpdateLastUpdatedAfterAsync(1, "realm", QboEntityType.Vendors.ToString(), It.IsAny<DateTime>()),
            Times.Once);
    }
}
