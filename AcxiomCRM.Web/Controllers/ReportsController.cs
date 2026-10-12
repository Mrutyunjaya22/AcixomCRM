using AcxiomCRM.Core.Constants;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(
        IReportService reportService,
        IUserService userService,
        ICurrentUserService currentUser)
    {
        _reportService = reportService;
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string report = "pipeline",
        string? status = null,
        string? stage = null,
        string? source = null,
        string? assignedToId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        // Permission check for restricted reports
        if (report == "audit" && !_currentUser.IsAdmin)
        {
            return Forbid();
        }

        if (report == "useractivity" && !(_currentUser.IsAdmin || _currentUser.IsManager))
        {
            return Forbid();
        }

        var salesUsers = await _userService.GetSalesUsersAsync();

        var model = new ReportIndexViewModel
        {
            ActiveReport = report.ToLower(),
            StatusFilter = status,
            StageFilter = stage,
            SourceFilter = source,
            AssignedToId = assignedToId,
            StartDate = startDate,
            EndDate = endDate,
            SalesUsersList = new SelectList(salesUsers, "Id", "FullName", assignedToId)
        };

        switch (model.ActiveReport)
        {
            case "pipeline":
                model.PipelineReport = await _reportService.GetPipelineReportAsync();
                break;
            case "conversion":
                model.ConversionReport = await _reportService.GetLeadConversionReportAsync();
                break;
            case "customer":
                model.CustomerReport = await _reportService.GetCustomerReportAsync(status, assignedToId);
                break;
            case "lead":
                model.LeadReport = await _reportService.GetLeadReportAsync(status, source, assignedToId);
                break;
            case "opportunity":
                model.OpportunityReport = await _reportService.GetOpportunityReportAsync(stage, status, assignedToId);
                break;
            case "followup":
                model.FollowUpReport = await _reportService.GetFollowUpReportAsync(status, assignedToId);
                break;
            case "useractivity":
                model.UserActivityReport = await _reportService.GetUserActivityReportAsync();
                break;
            case "audit":
                model.AuditReport = await _reportService.GetAuditReportAsync(status, null, startDate, endDate);
                break;
            default:
                model.ActiveReport = "pipeline";
                model.PipelineReport = await _reportService.GetPipelineReportAsync();
                break;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(
        string report = "pipeline",
        string? status = null,
        string? stage = null,
        string? source = null,
        string? assignedToId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (report == "audit" && !_currentUser.IsAdmin) return Forbid();
        if (report == "useractivity" && !(_currentUser.IsAdmin || _currentUser.IsManager)) return Forbid();

        byte[] fileBytes;
        string fileName;

        switch (report.ToLower())
        {
            case "customer":
                var customers = await _reportService.GetCustomerReportAsync(status, assignedToId);
                var custHeaders = new[] { "Code", "Name", "Email", "Phone", "Company", "City", "State", "Status", "Owner", "CreatedDate" };
                fileBytes = _reportService.ExportToCsv(customers, c => new[]
                {
                    c.CustomerCode,
                    c.CustomerName,
                    c.Email,
                    c.Phone,
                    c.CompanyName ?? "",
                    c.City ?? "",
                    c.State ?? "",
                    c.Status.ToString(),
                    c.AssignedTo?.FullName ?? "Unassigned",
                    c.CreatedDate.ToString("yyyy-MM-dd")
                }, custHeaders);
                fileName = "Customer_Report.csv";
                break;

            case "lead":
                var leads = await _reportService.GetLeadReportAsync(status, source, assignedToId);
                var leadHeaders = new[] { "Code", "Name", "Email", "Phone", "Company", "Source", "Status", "Priority", "ExpectedValue", "Owner", "CreatedDate" };
                fileBytes = _reportService.ExportToCsv(leads, l => new[]
                {
                    l.LeadCode,
                    l.LeadName,
                    l.Email,
                    l.Phone,
                    l.CompanyName ?? "",
                    l.Source.ToString(),
                    l.Status.ToString(),
                    l.Priority.ToString(),
                    l.ExpectedValue.ToString("F2"),
                    l.AssignedTo?.FullName ?? "Unassigned",
                    l.CreatedDate.ToString("yyyy-MM-dd")
                }, leadHeaders);
                fileName = "Lead_Report.csv";
                break;

            case "opportunity":
                var opps = await _reportService.GetOpportunityReportAsync(stage, status, assignedToId);
                var oppHeaders = new[] { "Code", "Name", "Customer", "Amount", "Stage", "Probability", "WeightedValue", "CloseDate", "Status", "Owner" };
                fileBytes = _reportService.ExportToCsv(opps, o => new[]
                {
                    o.OpportunityCode,
                    o.OpportunityName,
                    o.Customer?.CustomerName ?? "",
                    o.Amount.ToString("F2"),
                    o.Stage.ToString(),
                    $"{o.Probability}%",
                    o.WeightedValue.ToString("F2"),
                    o.ExpectedCloseDate.ToString("yyyy-MM-dd"),
                    o.Status.ToString(),
                    o.AssignedTo?.FullName ?? "Unassigned"
                }, oppHeaders);
                fileName = "Opportunity_Report.csv";
                break;

            case "followup":
                var followUps = await _reportService.GetFollowUpReportAsync(status, assignedToId);
                var fHeaders = new[] { "Date", "Type", "Subject", "Status", "RelatedTo", "AssignedUser" };
                fileBytes = _reportService.ExportToCsv(followUps, f => new[]
                {
                    f.FollowUpDate.ToString("yyyy-MM-dd"),
                    f.FollowUpType.ToString(),
                    f.Subject,
                    f.Status.ToString(),
                    f.RelatedType.ToString(),
                    f.AssignedTo?.FullName ?? "Unassigned"
                }, fHeaders);
                fileName = "FollowUp_Report.csv";
                break;

            case "pipeline":
            default:
                var pipeline = await _reportService.GetPipelineReportAsync();
                var pHeaders = new[] { "Pipeline Stage", "Deals Count", "Total Amount ($)", "Weighted Amount ($)" };
                fileBytes = _reportService.ExportToCsv(pipeline, p => new[]
                {
                    p.Stage,
                    p.Count.ToString(),
                    p.TotalAmount.ToString("F2"),
                    p.WeightedAmount.ToString("F2")
                }, pHeaders);
                fileName = "Sales_Pipeline_Report.csv";
                break;
        }

        return File(fileBytes, "text/csv", fileName);
    }
}
