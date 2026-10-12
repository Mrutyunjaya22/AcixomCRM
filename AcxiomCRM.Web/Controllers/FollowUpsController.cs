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
public class FollowUpsController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IOpportunityService _opportunityService;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public FollowUpsController(
        IFollowUpService followUpService,
        ICustomerService customerService,
        ILeadService leadService,
        IOpportunityService opportunityService,
        IUserService userService,
        ICurrentUserService currentUser)
    {
        _followUpService = followUpService;
        _customerService = customerService;
        _leadService = leadService;
        _opportunityService = opportunityService;
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        FollowUpStatus? status = null,
        FollowUpType? type = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? assignedToId = null,
        int page = 1)
    {
        var (followUps, totalCount) = await _followUpService.GetFollowUpsAsync(
            status: status,
            type: type,
            startDate: startDate,
            endDate: endDate,
            assignedToId: assignedToId,
            page: page,
            pageSize: 10);

        var upcoming = await _followUpService.GetUpcomingFollowUpsAsync(5);
        var overdue = await _followUpService.GetOverdueFollowUpsAsync(5);
        var salesUsers = await _userService.GetSalesUsersAsync();

        var model = new FollowUpListViewModel
        {
            FollowUps = followUps,
            UpcomingFollowUps = upcoming,
            OverdueFollowUps = overdue,
            Status = status,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            AssignedToId = assignedToId,
            CurrentPage = page,
            PageSize = 10,
            TotalCount = totalCount,
            SalesUsersList = new SelectList(salesUsers, "Id", "FullName", assignedToId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        var model = new FollowUpCreateEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.UtcNow.AddDays(1),
            RelatedType = leadId.HasValue ? FollowUpRelatedType.Lead :
                          opportunityId.HasValue ? FollowUpRelatedType.Opportunity :
                          FollowUpRelatedType.Customer
        };

        await PopulateDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpCreateEditViewModel model)
    {
        if (model.Status == FollowUpStatus.Planned && model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        try
        {
            var followUp = new FollowUp
            {
                RelatedType = model.RelatedType,
                CustomerId = model.RelatedType == FollowUpRelatedType.Customer ? model.CustomerId : null,
                LeadId = model.RelatedType == FollowUpRelatedType.Lead ? model.LeadId : null,
                OpportunityId = model.RelatedType == FollowUpRelatedType.Opportunity ? model.OpportunityId : null,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject,
                Remarks = model.Remarks,
                Status = model.Status,
                AssignedToId = model.AssignedToId
            };

            await _followUpService.CreateFollowUpAsync(followUp);
            TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' scheduled successfully.";
            return RedirectToAction(nameof(Index));
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
        var followUp = await _followUpService.GetFollowUpByIdAsync(id);
        if (followUp == null)
        {
            if (!await _followUpService.CanAccessFollowUpAsync(id)) return Forbid();
            return NotFound();
        }

        var model = new FollowUpCreateEditViewModel
        {
            FollowUpId = followUp.FollowUpId,
            RelatedType = followUp.RelatedType,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Subject = followUp.Subject,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedToId = followUp.AssignedToId
        };

        await PopulateDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUpCreateEditViewModel model)
    {
        if (id != model.FollowUpId) return BadRequest();

        if (model.Status == FollowUpStatus.Planned && model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        try
        {
            var followUp = new FollowUp
            {
                FollowUpId = model.FollowUpId,
                RelatedType = model.RelatedType,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject,
                Remarks = model.Remarks,
                Status = model.Status,
                AssignedToId = model.AssignedToId
            };

            await _followUpService.UpdateFollowUpAsync(followUp);
            TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' updated successfully.";
            return RedirectToAction(nameof(Index));
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
    public async Task<IActionResult> Complete(int id, string? remarks = null)
    {
        try
        {
            await _followUpService.CompleteFollowUpAsync(id, remarks);
            TempData["SuccessMessage"] = "Follow-up marked as Completed.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Reschedule(int id)
    {
        var followUp = await _followUpService.GetFollowUpByIdAsync(id);
        if (followUp == null)
        {
            if (!await _followUpService.CanAccessFollowUpAsync(id)) return Forbid();
            return NotFound();
        }

        var model = new FollowUpRescheduleViewModel
        {
            FollowUpId = followUp.FollowUpId,
            Subject = followUp.Subject,
            CurrentDate = followUp.FollowUpDate,
            NewDate = DateTime.UtcNow.AddDays(1)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, FollowUpRescheduleViewModel model)
    {
        if (id != model.FollowUpId) return BadRequest();

        if (model.NewDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(model.NewDate), "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _followUpService.RescheduleFollowUpAsync(id, model.NewDate, model.Remarks);
            TempData["SuccessMessage"] = "Follow-up rescheduled successfully.";
            return RedirectToAction(nameof(Index));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason = null)
    {
        try
        {
            await _followUpService.CancelFollowUpAsync(id, reason);
            TempData["SuccessMessage"] = "Follow-up cancelled.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task PopulateDropdowns(FollowUpCreateEditViewModel model)
    {
        var (customers, _) = await _customerService.GetCustomersAsync(pageSize: 100);
        var (leads, _) = await _leadService.GetLeadsAsync(pageSize: 100);
        var (opps, _) = await _opportunityService.GetOpportunitiesAsync(pageSize: 100);
        var salesUsers = await _userService.GetSalesUsersAsync();

        model.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", model.CustomerId);
        model.LeadsList = new SelectList(leads, "LeadId", "LeadName", model.LeadId);
        model.OpportunitiesList = new SelectList(opps, "OpportunityId", "OpportunityName", model.OpportunityId);
        model.SalesUsersList = new SelectList(salesUsers, "Id", "FullName", model.AssignedToId ?? _currentUser.UserId);
    }
}
