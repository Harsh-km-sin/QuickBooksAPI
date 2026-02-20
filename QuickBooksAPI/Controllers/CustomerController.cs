using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.Application.Interfaces;

namespace QuickBooksAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }
        [HttpGet("sync")]
        public async Task<IActionResult> SyncCustomers()
        {
            var result = await _customerService.GetCustomersAsync();
            return Ok(result);
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListCustomers([FromQuery] ListQueryParams? query = null)
        {
            query ??= new ListQueryParams();
            var result = await _customerService.ListCustomersAsync(query);
            return Ok(result);
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            var response = await _customerService.GetCustomerByIdAsync(id);
            return Ok(response);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
        {
            var response = await _customerService.CreateCustomerAsync(request);
            return Ok(response);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateCustomer([FromBody] UpdateCustomerRequest request)
        {
            var response = await _customerService.UpdateCustomerAsync(request);
            return Ok(response);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteCustomer([FromBody] DeleteCustomerRequest request)
        {
            var response = await _customerService.DeleteCustomerAsync(request);
            return Ok(response);
        }
    }
}
