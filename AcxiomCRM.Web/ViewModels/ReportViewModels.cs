using AcxiomCRM.Core.Entities;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.ViewModels;

public class ReportIndexViewModel
{
    public string ActiveReport { get; set; } = "pipeline";

    public List<PipelineReportItemDto> PipelineReport { get; set; } = new();
    public LeadConversionReportDto? ConversionReport { get; set; }
    public List<Customer> CustomerReport { get; set; } = new();
    public List<Lead> LeadReport { get; set; } = new();
    public List<Opportunity> OpportunityReport { get; set; } = new();
    public List<FollowUp> FollowUpReport { get; set; } = new();
    public List<UserActivityReportItemDto> UserActivityReport { get; set; } = new();
    public List<AuditLog> AuditReport { get; set; } = new();

    public string? StatusFilter { get; set; }
    public string? StageFilter { get; set; }
    public string? SourceFilter { get; set; }
    public string? AssignedToId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public SelectList? SalesUsersList { get; set; }
}
