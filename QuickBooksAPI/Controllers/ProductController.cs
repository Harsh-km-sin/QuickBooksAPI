using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productServices;
        public ProductController(IProductService productServices)
        {
            _productServices = productServices;
        }

        [HttpGet("sync")]
        public async Task<IActionResult> SyncProducts()
        {
            var result = await _productServices.GetProductsAsync();
            return Ok(result);
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListProducts()
        {
            var result = await _productServices.ListProductsAsync();
            return Ok(result);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            var response = await _productServices.CreateProductAsync(request);
            return Ok(response);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductRequest request)
        {
            var response = await _productServices.UpdateProductAsync(request);
            return Ok(response);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteProduct([FromBody] DeleteProductRequest request)
        {
            var response = await _productServices.DeleteProductAsync(request);
            return Ok(response);
        }
    }
}
