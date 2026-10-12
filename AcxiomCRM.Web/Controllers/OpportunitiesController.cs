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
public class OpportunitiesController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        ICustomerService customerService,
        ILeadService leadService,
        IUserService userService,
        ICurrentUserService currentUser)
    {
        _opportunityService = opportunityService;
        _customerService = customerService;
        _leadService = leadService;
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        OpportunityStage? stage = null,
        OpportunityStatus? status = null,
        string? assignedToId = null,
        int? customerId = null,
        string? sortBy = null,
        bool desc = false,
        int page = 1)
    {
        var (opportunities, totalCount) = await _opportunityService.GetOpportunitiesAsync(
            searchTerm: search,
            stage: stage,
            status: status,
            assignedToId: assignedToId,
            customerId: customerId,
            sortBy: sortBy,
            sortDescending: desc,
            page: page,
            pageSize: 10);

        var salesUsers = await _userService.GetSalesUsersAsync();

        var model = new OpportunityListViewModel
        {
            Opportunities = opportunities,
            SearchTerm = search,
            Stage = stage,
            Status = status,
            AssignedToId = assignedToId,
            CustomerId = customerId,
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
        var opportunity = await _opportunityService.GetOpportunityWithDetailsAsync(id);
        if (opportunity == null)
        {
            if (!await _opportunityService.CanAccessOpportunityAsync(id)) return Forbid();
            return NotFound();
        }

        var model = new OpportunityDetailsViewModel
        {
            Opportunity = opportunity,
            CanEdit = await _opportunityService.CanAccessOpportunityAsync(id)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var (customers, _) = await _customerService.GetCustomersAsync(pageSize: 100);
        var (leads, _) = await _leadService.GetLeadsAsync(pageSize: 100);
        var salesUsers = await _userService.GetSalesUsersAsync();

        var model = new OpportunityCreateEditViewModel
        {
            CustomerId = customerId ?? (customers.FirstOrDefault()?.CustomerId ?? 0),
            LeadId = leadId,
            ExpectedCloseDate = DateTime.UtcNow.AddMonths(1),
            CustomersList = new SelectList(customers, "CustomerId", "CustomerName", customerId),
            LeadsList = new SelectList(leads, "LeadId", "LeadName", leadId),
            SalesUsersList = new SelectList(salesUsers, "Id", "FullName", _currentUser.UserId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityCreateEditViewModel model)
    {
        if (model.Amount <= 0)
        {
            ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
        }

        if (model.Stage != OpportunityStage.Won && model.Stage != OpportunityStage.Lost)
        {
            if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        try
        {
            var opportunity = new Opportunity
            {
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                Notes = model.Notes,
                AssignedToId = model.AssignedToId
            };

            var created = await _opportunityService.CreateOpportunityAsync(opportunity);
            TempData["SuccessMessage"] = $"Opportunity '{created.OpportunityName}' ({created.OpportunityCode}) created successfully.";
            return RedirectToAction(nameof(Details), new { id = created.OpportunityId });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateDropdowns(model);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opportunity = await _opportunityService.GetOpportunityByIdAsync(id);
        if (opportunity == null)
        {
            if (!await _opportunityService.CanAccessOpportunityAsync(id)) return Forbid();
            return NotFound();
        }

        var (customers, _) = await _customerService.GetCustomersAsync(pageSize: 100);
        var (leads, _) = await _leadService.GetLeadsAsync(pageSize: 100);
        var salesUsers = await _userService.GetSalesUsersAsync();

        var model = new OpportunityCreateEditViewModel
        {
            OpportunityId = opportunity.OpportunityId,
            OpportunityCode = opportunity.OpportunityCode,
            OpportunityName = opportunity.OpportunityName,
            CustomerId = opportunity.CustomerId,
            LeadId = opportunity.LeadId,
            Amount = opportunity.Amount,
            Stage = opportunity.Stage,
            Probability = opportunity.Probability,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            Status = opportunity.Status,
            Notes = opportunity.Notes,
            AssignedToId = opportunity.AssignedToId,
            CustomersList = new SelectList(customers, "CustomerId", "CustomerName", opportunity.CustomerId),
            LeadsList = new SelectList(leads, "LeadId", "LeadName", opportunity.LeadId),
            SalesUsersList = new SelectList(salesUsers, "Id", "FullName", opportunity.AssignedToId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityCreateEditViewModel model)
    {
        if (id != model.OpportunityId) return BadRequest();

        if (model.Amount <= 0)
        {
            ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
        }

        if (model.Stage != OpportunityStage.Won && model.Stage != OpportunityStage.Lost)
        {
            if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        try
        {
            var opportunity = new Opportunity
            {
                OpportunityId = model.OpportunityId,
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                Notes = model.Notes,
                AssignedToId = model.AssignedToId
            };

            await _opportunityService.UpdateOpportunityAsync(opportunity);
            TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = opportunity.OpportunityId });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateDropdowns(model);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStage(int id, OpportunityStage stage)
    {
        try
        {
            await _opportunityService.UpdateStageAsync(id, stage);
            TempData["SuccessMessage"] = $"Opportunity stage moved to {stage}.";
            return RedirectToAction(nameof(Details), new { id });
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkLost(int id)
    {
        try
        {
            var success = await _opportunityService.DeleteOrDeactivateOpportunityAsync(id);
            if (!success) return NotFound();

            TempData["SuccessMessage"] = "Opportunity marked as Lost.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private async Task PopulateDropdowns(OpportunityCreateEditViewModel model)
    {
        var (customers, _) = await _customerService.GetCustomersAsync(pageSize: 100);
        var (leads, _) = await _leadService.GetLeadsAsync(pageSize: 100);
        var salesUsers = await _userService.GetSalesUsersAsync();

        model.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", model.CustomerId);
        model.LeadsList = new SelectList(leads, "LeadId", "LeadName", model.LeadId);
        model.SalesUsersList = new SelectList(salesUsers, "Id", "FullName", model.AssignedToId);
    }
}
