using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Produces("application/json")]
[Authorize]
[IgnoreAntiforgeryToken]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersApiController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (customers, totalCount) = await _customerService.GetCustomersAsync(
            searchTerm: search,
            page: page,
            pageSize: pageSize);

        var dtos = customers.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            AssignedToId = c.AssignedToId,
            AssignedToName = c.AssignedTo?.FullName,
            CreatedDate = c.CreatedDate
        }).ToList();

        return Ok(ApiResponse<List<CustomerDto>>.Ok(dtos, $"Retrieved {dtos.Count} of {totalCount} customers."));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            if (!await _customerService.CanAccessCustomerAsync(id)) return Forbid();
            return NotFound(ApiResponse<string>.Fail("Customer not found."));
        }

        var dto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            AssignedToId = customer.AssignedToId,
            AssignedToName = customer.AssignedTo?.FullName,
            CreatedDate = customer.CreatedDate
        };

        return Ok(ApiResponse<CustomerDto>.Ok(dto));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var customer = new Customer
            {
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Status = dto.Status,
                Notes = dto.Notes,
                AssignedToId = dto.AssignedToId
            };

            var created = await _customerService.CreateCustomerAsync(customer);

            var resultDto = new CustomerDto
            {
                CustomerId = created.CustomerId,
                CustomerCode = created.CustomerCode,
                CustomerName = created.CustomerName,
                Email = created.Email,
                Phone = created.Phone,
                CompanyName = created.CompanyName,
                Address = created.Address,
                City = created.City,
                State = created.State,
                Status = created.Status,
                AssignedToId = created.AssignedToId,
                CreatedDate = created.CreatedDate
            };

            return CreatedAtAction(nameof(GetCustomerById), new { id = created.CustomerId }, ApiResponse<CustomerDto>.Ok(resultDto, "Customer created."));
        }
        catch (DomainException ex)
        {
            return Conflict(ApiResponse<string>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var customer = new Customer
            {
                CustomerId = id,
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Status = dto.Status,
                Notes = dto.Notes,
                AssignedToId = dto.AssignedToId
            };

            var updated = await _customerService.UpdateCustomerAsync(customer);

            var resultDto = new CustomerDto
            {
                CustomerId = updated.CustomerId,
                CustomerCode = updated.CustomerCode,
                CustomerName = updated.CustomerName,
                Email = updated.Email,
                Phone = updated.Phone,
                CompanyName = updated.CompanyName,
                Address = updated.Address,
                City = updated.City,
                State = updated.State,
                Status = updated.Status,
                AssignedToId = updated.AssignedToId,
                CreatedDate = updated.CreatedDate
            };

            return Ok(ApiResponse<CustomerDto>.Ok(resultDto, "Customer updated."));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        try
        {
            var success = await _customerService.DeleteOrDeactivateCustomerAsync(id);
            if (!success) return NotFound(ApiResponse<string>.Fail("Customer not found."));

            return Ok(ApiResponse<string>.Ok("Customer deactivated."));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
