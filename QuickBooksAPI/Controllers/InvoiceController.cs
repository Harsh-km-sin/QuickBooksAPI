using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListInvoices()
        {
            var result = await _invoiceService.ListInvoicesAsync();
            return Ok(result);
        }

        [HttpGet("sync")]
        public async Task<IActionResult> SyncInvoices()
        {
            var response = await _invoiceService.SyncInvoicesAsync();
            return Ok(response);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
        {
            var response = await _invoiceService.CreateInvoiceAsync(request);
            return Ok(response);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateInvoice([FromBody] UpdateInvoiceRequest request)
        {
            var response = await _invoiceService.UpdateInvoiceAsync(request);
            return Ok(response);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteInvoice([FromBody] DeleteInvoiceRequest request)
        {
            var response = await _invoiceService.DeleteInvoiceAsync(request);
            return Ok(response);
        }

        [HttpPost("void")]
        public async Task<IActionResult> VoidInvoice([FromBody] VoidInvoiceRequest request)
        {
            var response = await _invoiceService.VoidInvoiceAsync(request);
            return Ok(response);
        }
    }
}
