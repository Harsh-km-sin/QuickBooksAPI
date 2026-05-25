using Moq;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Products.Handlers;

namespace QuickBooksAPI.UnitTests.Products;

/// <summary>
/// Locks list/paging behavior for <see cref="ListProductsHandler"/>.
/// </summary>
public sealed class ProductServicesListCharacterizationTests
{
    [Fact]
    public async Task ListProductsAsync_WithoutUserContext_ReturnsFail()
    {
        var ctx = new Mock<IRequestContext>();
        ctx.Setup(c => c.UserId).Returns((string?)null);
        ctx.Setup(c => c.RealmId).Returns("realm");

        var sut = new ListProductsHandler(
            ctx.Object,
            Mock.Of<IProductRepository>());

        var result = await sut.HandleListAsync();

        Assert.False(result.Success);
        Assert.Contains("sign in", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListProductsAsync_ReturnsMappedProductsFromRepository()
    {
        var ctx = new Mock<IRequestContext>();
        ctx.Setup(c => c.UserId).Returns("42");
        ctx.Setup(c => c.RealmId).Returns("realm-1");

        var row = new ProductDto
        {
            Id = 1,
            QboId = "qbo-1",
            Name = "Item A",
            UserId = 42,
            RealmId = "realm-1"
        };

        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetAllByUserAndRealmAsync(42, "realm-1"))
            .ReturnsAsync(new List<ProductDto> { row });

        var sut = new ListProductsHandler(ctx.Object, repo.Object);

        var result = await sut.HandleListAsync();

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        var list = result.Data!.ToList();
        Assert.Single(list);
        Assert.Equal("Item A", list[0].Name);
        Assert.Equal("qbo-1", list[0].QboId);
    }

    [Fact]
    public async Task ListProductsAsync_Paged_PassesQueryToRepository()
    {
        var ctx = new Mock<IRequestContext>();
        ctx.Setup(c => c.UserId).Returns("1");
        ctx.Setup(c => c.RealmId).Returns("r");

        var repo = new Mock<IProductRepository>();
        var paged = new QuickBooksAPI.API.DTOs.Response.PagedResult<ProductDto>
        {
            Items = Array.Empty<ProductDto>(),
            TotalCount = 0,
            Page = 2,
            PageSize = 10
        };
        repo.Setup(r => r.GetPagedByUserAndRealmAsync(1, "r", 2, 10, "find", null))
            .ReturnsAsync(paged);

        var sut = new ListProductsHandler(ctx.Object, repo.Object);

        var query = new ListQueryParams { Page = 2, PageSize = 10, Search = "find" };
        var result = await sut.HandlePagedAsync(query);

        Assert.True(result.Success);
        repo.Verify(
            r => r.GetPagedByUserAndRealmAsync(1, "r", 2, 10, "find", null),
            Times.Once);
    }
}
