using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public CustomersController(
        ICustomerService customerService,
        IUserService userService,
        ICurrentUserService currentUser)
    {
        _customerService = customerService;
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        CustomerStatus? status = null,
        string? assignedToId = null,
        string? sortBy = null,
        bool desc = false,
        int page = 1)
    {
        var (customers, totalCount) = await _customerService.GetCustomersAsync(
            searchTerm: search,
            status: status,
            assignedToId: assignedToId,
            sortBy: sortBy,
            sortDescending: desc,
            page: page,
            pageSize: 10);

        var salesUsers = await _userService.GetSalesUsersAsync();

        var model = new CustomerListViewModel
        {
            Customers = customers,
            SearchTerm = search,
            Status = status,
            AssignedToId = assignedToId,
            SortBy = sortBy,
            SortDescending = desc,
            CurrentPage = page,
            PageSize = 10,
            TotalCount = totalCount,
            SalesUsersList = new SelectList(salesUsers, "Id", "FullName", assignedToId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customerService.GetCustomerWithDetailsAsync(id);
        if (customer == null)
        {
            if (!await _customerService.CanAccessCustomerAsync(id))
            {
                return Forbid();
            }
            return NotFound();
        }

        var model = new CustomerDetailsViewModel
        {
            Customer = customer,
            CanEdit = await _customerService.CanAccessCustomerAsync(id)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var salesUsers = await _userService.GetSalesUsersAsync();
        var model = new CustomerCreateEditViewModel
        {
            SalesUsersList = new SelectList(salesUsers, "Id", "FullName", _currentUser.UserId)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCreateEditViewModel model)
    {
        if (!await _customerService.IsEmailUniqueAsync(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "A customer with this email address already exists.");
        }

        if (!await _customerService.IsPhoneUniqueAsync(model.Phone))
        {
            ModelState.AddModelError(nameof(model.Phone), "A customer with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            var salesUsers = await _userService.GetSalesUsersAsync();
            model.SalesUsersList = new SelectList(salesUsers, "Id", "FullName", model.AssignedToId);
            return View(model);
        }

        try
        {
            var customer = new Customer
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Status = model.Status,
                Notes = model.Notes,
                AssignedToId = model.AssignedToId
            };

            var created = await _customerService.CreateCustomerAsync(customer);
            TempData["SuccessMessage"] = $"Customer '{created.CustomerName}' ({created.CustomerCode}) created successfully.";
            return RedirectToAction(nameof(Details), new { id = created.CustomerId });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var salesUsers = await _userService.GetSalesUsersAsync();
            model.SalesUsersList = new SelectList(salesUsers, "Id", "FullName", model.AssignedToId);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            if (!await _customerService.CanAccessCustomerAsync(id)) return Forbid();
            return NotFound();
        }

        var salesUsers = await _userService.GetSalesUsersAsync();
        var model = new CustomerCreateEditViewModel
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
            Notes = customer.Notes,
            AssignedToId = customer.AssignedToId,
            SalesUsersList = new SelectList(salesUsers, "Id", "FullName", customer.AssignedToId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerCreateEditViewModel model)
    {
        if (id != model.CustomerId) return BadRequest();

        if (!await _customerService.IsEmailUniqueAsync(model.Email, id))
        {
            ModelState.AddModelError(nameof(model.Email), "Another customer with this email address already exists.");
        }

        if (!await _customerService.IsPhoneUniqueAsync(model.Phone, id))
        {
            ModelState.AddModelError(nameof(model.Phone), "Another customer with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            var salesUsers = await _userService.GetSalesUsersAsync();
            model.SalesUsersList = new SelectList(salesUsers, "Id", "FullName", model.AssignedToId);
            return View(model);
        }

        try
        {
            var customer = new Customer
            {
                CustomerId = model.CustomerId,
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Status = model.Status,
                Notes = model.Notes,
                AssignedToId = model.AssignedToId
            };

            await _customerService.UpdateCustomerAsync(customer);
            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = customer.CustomerId });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var salesUsers = await _userService.GetSalesUsersAsync();
            model.SalesUsersList = new SelectList(salesUsers, "Id", "FullName", model.AssignedToId);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            var success = await _customerService.DeleteOrDeactivateCustomerAsync(id);
            if (!success) return NotFound();

            TempData["SuccessMessage"] = "Customer was deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
