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
public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public ActivitiesController(
        IActivityService activityService,
        ICustomerService customerService,
        ILeadService leadService,
        IUserService userService,
        ICurrentUserService currentUser)
    {
        _activityService = activityService;
        _customerService = customerService;
        _leadService = leadService;
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        ActivityType? type = null,
        ActivityStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? assignedToId = null,
        int page = 1)
    {
        var (activities, totalCount) = await _activityService.GetActivitiesAsync(
            type: type,
            status: status,
            startDate: startDate,
            endDate: endDate,
            assignedToId: assignedToId,
            page: page,
            pageSize: 10);

        var salesUsers = await _userService.GetSalesUsersAsync();

        var model = new ActivityListViewModel
        {
            Activities = activities,
            Type = type,
            Status = status,
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
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var model = new ActivityCreateEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.UtcNow
        };

        await PopulateDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        try
        {
            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject,
                Description = model.Description,
                ActivityDate = model.ActivityDate,
                Status = model.Status,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                AssignedToId = model.AssignedToId
            };

            await _activityService.CreateActivityAsync(activity);
            TempData["SuccessMessage"] = $"Activity '{activity.Subject}' logged successfully.";
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
        var activity = await _activityService.GetActivityByIdAsync(id);
        if (activity == null) return NotFound();

        var model = new ActivityCreateEditViewModel
        {
            ActivityId = activity.ActivityId,
            ActivityType = activity.ActivityType,
            Subject = activity.Subject,
            Description = activity.Description,
            ActivityDate = activity.ActivityDate,
            Status = activity.Status,
            CustomerId = activity.CustomerId,
            LeadId = activity.LeadId,
            AssignedToId = activity.AssignedToId
        };

        await PopulateDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityCreateEditViewModel model)
    {
        if (id != model.ActivityId) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        try
        {
            var activity = new Activity
            {
                ActivityId = model.ActivityId,
                ActivityType = model.ActivityType,
                Subject = model.Subject,
                Description = model.Description,
                ActivityDate = model.ActivityDate,
                Status = model.Status,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                AssignedToId = model.AssignedToId
            };

            await _activityService.UpdateActivityAsync(activity);
            TempData["SuccessMessage"] = $"Activity '{activity.Subject}' updated successfully.";
            return RedirectToAction(nameof(Index));
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
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _activityService.DeleteActivityAsync(id);
        if (!success) return NotFound();

        TempData["SuccessMessage"] = "Activity deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns(ActivityCreateEditViewModel model)
    {
        var (customers, _) = await _customerService.GetCustomersAsync(pageSize: 100);
        var (leads, _) = await _leadService.GetLeadsAsync(pageSize: 100);
        var salesUsers = await _userService.GetSalesUsersAsync();

        model.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", model.CustomerId);
        model.LeadsList = new SelectList(leads, "LeadId", "LeadName", model.LeadId);
        model.SalesUsersList = new SelectList(salesUsers, "Id", "FullName", model.AssignedToId ?? _currentUser.UserId);
    }
}
