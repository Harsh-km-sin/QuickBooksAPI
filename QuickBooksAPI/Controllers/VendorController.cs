using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VendorController : ControllerBase
    {
        private readonly IVendorService _vendorService;

        public VendorController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListVendors([FromQuery] ListQueryParams? query = null)
        {
            query ??= new ListQueryParams();
            var result = await _vendorService.ListVendorsAsync(query);
            return Ok(result);
        }

        [HttpGet("sync")]
        public async Task<IActionResult> GetVendors()
        {
            var response = await _vendorService.GetVendorsAsync();
            return Ok(response);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request)
        {
            var response = await _vendorService.CreateVendorAsync(request);
            return Ok(response);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateVendor([FromBody] UpdateVendorRequest request)
        {
            var response = await _vendorService.UpdatevendorAsync(request);
            return Ok(response);
        }

        [HttpDelete("softDelete")]
        public async Task<IActionResult> SoftDeleteVendor([FromBody] SoftDeleteVendorRequest request)
        {
            var response = await _vendorService.SoftDeleteVendorAsync(request);
            return Ok(response);
        }
    }
}
