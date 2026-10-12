using AcxiomCRM.Core.Entities;

namespace AcxiomCRM.Infrastructure.Services.Interfaces;

public interface IReportService
{
    Task<List<Customer>> GetCustomerReportAsync(string? status = null, string? assignedToId = null);
    Task<List<Lead>> GetLeadReportAsync(string? status = null, string? source = null, string? assignedToId = null);
    Task<List<Opportunity>> GetOpportunityReportAsync(string? stage = null, string? status = null, string? assignedToId = null);
    Task<List<FollowUp>> GetFollowUpReportAsync(string? status = null, string? assignedToId = null);
    Task<List<PipelineReportItemDto>> GetPipelineReportAsync();
    Task<LeadConversionReportDto> GetLeadConversionReportAsync();
    Task<List<UserActivityReportItemDto>> GetUserActivityReportAsync();
    Task<List<AuditLog>> GetAuditReportAsync(string? module = null, string? action = null, DateTime? startDate = null, DateTime? endDate = null);
    byte[] ExportToCsv<T>(IEnumerable<T> data, Func<T, string[]> rowMapper, string[] headers);
}

public class PipelineReportItemDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class LeadConversionReportDto
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public int LostLeads { get; set; }
    public int OpenLeads { get; set; }
    public double ConversionRatePercentage { get; set; }
}

public class UserActivityReportItemDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int CustomersAssigned { get; set; }
    public int LeadsAssigned { get; set; }
    public int OpportunitiesAssigned { get; set; }
    public int FollowUpsCompleted { get; set; }
    public int ActivitiesLogged { get; set; }
}
