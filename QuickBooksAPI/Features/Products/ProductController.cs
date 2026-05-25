using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Features.Products.Handlers;

namespace QuickBooksAPI.Features.Products;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly SyncProductsHandler _syncProducts;
    private readonly ListProductsHandler _listProducts;
    private readonly CreateProductHandler _createProduct;
    private readonly UpdateProductHandler _updateProduct;
    private readonly DeleteProductHandler _deleteProduct;

    public ProductController(
        SyncProductsHandler syncProducts,
        ListProductsHandler listProducts,
        CreateProductHandler createProduct,
        UpdateProductHandler updateProduct,
        DeleteProductHandler deleteProduct)
    {
        _syncProducts = syncProducts;
        _listProducts = listProducts;
        _createProduct = createProduct;
        _updateProduct = updateProduct;
        _deleteProduct = deleteProduct;
    }

    [HttpGet("sync")]
    public async Task<IActionResult> SyncProducts()
    {
        var result = await _syncProducts.HandleAsync();
        return Ok(result);
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListProducts([FromQuery] ListQueryParams? query = null)
    {
        query ??= new ListQueryParams();
        var result = await _listProducts.HandlePagedAsync(query);
        return Ok(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var response = await _createProduct.HandleAsync(request);
        return Ok(response);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductRequest request)
    {
        var response = await _updateProduct.HandleAsync(request);
        return Ok(response);
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteProduct([FromBody] DeleteProductRequest request)
    {
        var response = await _deleteProduct.HandleAsync(request);
        return Ok(response);
    }
}
