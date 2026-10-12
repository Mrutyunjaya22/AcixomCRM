using AcxiomCRM.Core.Constants;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AuditLogsController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly IUserService _userService;

    public AuditLogsController(
        IAuditLogService auditLogService,
        IUserService userService)
    {
        _auditLogService = auditLogService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? userId = null,
        string? module = null,
        string? actionName = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1)
    {
        var (logs, totalCount) = await _auditLogService.GetAuditLogsAsync(
            userId: userId,
            module: module,
            action: actionName,
            startDate: startDate,
            endDate: endDate,
            page: page,
            pageSize: 20);

        var distinctModules = await _auditLogService.GetDistinctModulesAsync();
        var distinctActions = await _auditLogService.GetDistinctActionsAsync();
        var users = await _userService.GetUsersAsync();

        var model = new AuditLogListViewModel
        {
            Logs = logs,
            SelectedUserId = userId,
            SelectedModule = module,
            SelectedAction = actionName,
            StartDate = startDate,
            EndDate = endDate,
            CurrentPage = page,
            PageSize = 20,
            TotalCount = totalCount,
            ModulesList = new SelectList(distinctModules, module),
            ActionsList = new SelectList(distinctActions, actionName),
            UsersList = new SelectList(users, "Id", "FullName", userId)
        };

        return View(model);
    }
}
