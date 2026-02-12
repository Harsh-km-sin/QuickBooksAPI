using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BillController : ControllerBase
    {
        private readonly IBillService _billService;

        public BillController(IBillService billService)
        {
            _billService = billService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListBills()
        {
            var result = await _billService.ListBillsAsync();
            return Ok(result);
        }

        [HttpGet("sync")]
        public async Task<IActionResult> SyncBills()
        {
            var response = await _billService.SyncBillsAsync();
            return Ok(response);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBill([FromBody] CreateBillRequest request)
        {
            var response = await _billService.CreateBillAsync(request);
            return Ok(response);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateBill([FromBody] UpdateBillRequest request)
        {
            var response = await _billService.UpdateBillAsync(request);
            return Ok(response);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteBill([FromBody] DeleteBillRequest request)
        {
            var response = await _billService.DeleteBillAsync(request);
            return Ok(response);
        }
    }
}
