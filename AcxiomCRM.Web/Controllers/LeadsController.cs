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
public class LeadsController : Controller
{
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public LeadsController(
        ILeadService leadService,
        IUserService userService,
        ICurrentUserService currentUser)
    {
        _leadService = leadService;
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        LeadStatus? status = null,
        LeadSource? source = null,
        string? assignedToId = null,
        string? sortBy = null,
        bool desc = false,
        int page = 1)
    {
        var (leads, totalCount) = await _leadService.GetLeadsAsync(
            searchTerm: search,
            status: status,
            source: source,
            assignedToId: assignedToId,
            sortBy: sortBy,
            sortDescending: desc,
            page: page,
            pageSize: 10);

        var salesUsers = await _userService.GetSalesUsersAsync();

        var model = new LeadListViewModel
        {
            Leads = leads,
            SearchTerm = search,
            Status = status,
            Source = source,
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
        var lead = await _leadService.GetLeadWithDetailsAsync(id);
        if (lead == null)
        {
            if (!await _leadService.CanAccessLeadAsync(id)) return Forbid();
            return NotFound();
        }

        var model = new LeadDetailsViewModel
        {
            Lead = lead,
            CanEdit = await _leadService.CanAccessLeadAsync(id)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var salesUsers = await _userService.GetSalesUsersAsync();
        var model = new LeadCreateEditViewModel
        {
            SalesUsersList = new SelectList(salesUsers, "Id", "FullName", _currentUser.UserId)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadCreateEditViewModel model)
    {
        if (model.ExpectedValue < 0)
        {
            ModelState.AddModelError(nameof(model.ExpectedValue), "Expected Value cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            var salesUsers = await _userService.GetSalesUsersAsync();
            model.SalesUsersList = new SelectList(salesUsers, "Id", "FullName", model.AssignedToId);
            return View(model);
        }

        try
        {
            var lead = new Lead
            {
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                Notes = model.Notes,
                AssignedToId = model.AssignedToId
            };

            var created = await _leadService.CreateLeadAsync(lead);
            TempData["SuccessMessage"] = $"Lead '{created.LeadName}' ({created.LeadCode}) created successfully.";
            return RedirectToAction(nameof(Details), new { id = created.LeadId });
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
        var lead = await _leadService.GetLeadByIdAsync(id);
        if (lead == null)
        {
            if (!await _leadService.CanAccessLeadAsync(id)) return Forbid();
            return NotFound();
        }

        if (lead.Status == LeadStatus.Converted)
        {
            TempData["ErrorMessage"] = "Converted leads cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var salesUsers = await _userService.GetSalesUsersAsync();
        var model = new LeadCreateEditViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            Notes = lead.Notes,
            AssignedToId = lead.AssignedToId,
            SalesUsersList = new SelectList(salesUsers, "Id", "FullName", lead.AssignedToId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeadCreateEditViewModel model)
    {
        if (id != model.LeadId) return BadRequest();

        if (model.ExpectedValue < 0)
        {
            ModelState.AddModelError(nameof(model.ExpectedValue), "Expected Value cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            var salesUsers = await _userService.GetSalesUsersAsync();
            model.SalesUsersList = new SelectList(salesUsers, "Id", "FullName", model.AssignedToId);
            return View(model);
        }

        try
        {
            var lead = new Lead
            {
                LeadId = model.LeadId,
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                Notes = model.Notes,
                AssignedToId = model.AssignedToId
            };

            await _leadService.UpdateLeadAsync(lead);
            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = lead.LeadId });
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
    public async Task<IActionResult> MarkLost(int id)
    {
        try
        {
            var success = await _leadService.DeleteOrDeactivateLeadAsync(id);
            if (!success) return NotFound();

            TempData["SuccessMessage"] = "Lead was marked as Lost.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(id);
        if (lead == null)
        {
            if (!await _leadService.CanAccessLeadAsync(id)) return Forbid();
            return NotFound();
        }

        if (lead.Status == LeadStatus.Converted)
        {
            TempData["ErrorMessage"] = "This lead is already converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            LeadName = lead.LeadName,
            Email = lead.Email,
            CompanyName = lead.CompanyName,
            ExpectedValue = lead.ExpectedValue
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id, LeadConvertViewModel model)
    {
        if (id != model.LeadId) return BadRequest();

        try
        {
            var (customer, opportunity) = await _leadService.ConvertLeadAsync(id, model.Notes);
            TempData["SuccessMessage"] = $"Lead successfully converted! Customer '{customer.CustomerName}' ({customer.CustomerCode}) created" +
                (opportunity != null ? $" and Opportunity '{opportunity.OpportunityName}' ({opportunity.OpportunityCode}) generated." : ".");

            return RedirectToAction("Details", "Customers", new { id = customer.CustomerId });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
