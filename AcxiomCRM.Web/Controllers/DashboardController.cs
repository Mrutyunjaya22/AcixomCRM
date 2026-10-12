using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IFollowUpService _followUpService;

    public DashboardController(
        IDashboardService dashboardService,
        IFollowUpService followUpService)
    {
        _dashboardService = dashboardService;
        _followUpService = followUpService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? dateFilter = "All", DateTime? customStart = null, DateTime? customEnd = null)
    {
        var metrics = await _dashboardService.GetDashboardMetricsAsync(dateFilter, customStart, customEnd);
        ViewBag.UpcomingFollowUps = await _followUpService.GetUpcomingFollowUpsAsync(5);
        ViewBag.OverdueFollowUps = await _followUpService.GetOverdueFollowUpsAsync(5);

        return View(metrics);
    }
}
