using System.Text;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Infrastructure.Services.Implementations;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<List<Customer>> GetCustomerReportAsync(string? status = null, string? assignedToId = null)
    {
        var query = _context.Customers
            .Include(c => c.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            query = query.Where(c => c.AssignedToId == _currentUser.UserId);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CustomerStatus>(status, true, out var st))
        {
            query = query.Where(c => c.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(c => c.AssignedToId == assignedToId);
        }

        return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public async Task<List<Lead>> GetLeadReportAsync(string? status = null, string? source = null, string? assignedToId = null)
    {
        var query = _context.Leads
            .Include(l => l.AssignedTo)
            .Include(l => l.ConvertedCustomer)
            .AsNoTracking()
            .AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            query = query.Where(l => l.AssignedToId == _currentUser.UserId);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeadStatus>(status, true, out var st))
        {
            query = query.Where(l => l.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(source) && Enum.TryParse<LeadSource>(source, true, out var src))
        {
            query = query.Where(l => l.Source == src);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(l => l.AssignedToId == assignedToId);
        }

        return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
    }

    public async Task<List<Opportunity>> GetOpportunityReportAsync(string? stage = null, string? status = null, string? assignedToId = null)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            query = query.Where(o => o.AssignedToId == _currentUser.UserId);
        }

        if (!string.IsNullOrWhiteSpace(stage) && Enum.TryParse<OpportunityStage>(stage, true, out var stg))
        {
            query = query.Where(o => o.Stage == stg);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OpportunityStatus>(status, true, out var sts))
        {
            query = query.Where(o => o.Status == sts);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(o => o.AssignedToId == assignedToId);
        }

        return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
    }

    public async Task<List<FollowUp>> GetFollowUpReportAsync(string? status = null, string? assignedToId = null)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            query = query.Where(f => f.AssignedToId == _currentUser.UserId);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FollowUpStatus>(status, true, out var st))
        {
            query = query.Where(f => f.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(f => f.AssignedToId == assignedToId);
        }

        return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
    }

    public async Task<List<PipelineReportItemDto>> GetPipelineReportAsync()
    {
        var oppsQuery = _context.Opportunities.AsNoTracking().AsQueryable();
        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            oppsQuery = oppsQuery.Where(o => o.AssignedToId == _currentUser.UserId);
        }

        var stages = new[]
        {
            OpportunityStage.Qualification,
            OpportunityStage.Proposal,
            OpportunityStage.Negotiation,
            OpportunityStage.Won,
            OpportunityStage.Lost
        };

        var list = new List<PipelineReportItemDto>();
        var items = await oppsQuery.ToListAsync();

        foreach (var stage in stages)
        {
            var stageOpps = items.Where(o => o.Stage == stage).ToList();
            list.Add(new PipelineReportItemDto
            {
                Stage = stage.ToString(),
                Count = stageOpps.Count,
                TotalAmount = stageOpps.Sum(o => o.Amount),
                WeightedAmount = stageOpps.Sum(o => o.WeightedValue)
            });
        }

        return list;
    }

    public async Task<LeadConversionReportDto> GetLeadConversionReportAsync()
    {
        var leadsQuery = _context.Leads.AsNoTracking().AsQueryable();
        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            leadsQuery = leadsQuery.Where(l => l.AssignedToId == _currentUser.UserId);
        }

        var total = await leadsQuery.CountAsync();
        var converted = await leadsQuery.CountAsync(l => l.Status == LeadStatus.Converted);
        var lost = await leadsQuery.CountAsync(l => l.Status == LeadStatus.Lost);
        var open = total - converted - lost;
        var rate = total > 0 ? Math.Round((double)converted / total * 100, 2) : 0;

        return new LeadConversionReportDto
        {
            TotalLeads = total,
            ConvertedLeads = converted,
            LostLeads = lost,
            OpenLeads = open,
            ConversionRatePercentage = rate
        };
    }

    public async Task<List<UserActivityReportItemDto>> GetUserActivityReportAsync()
    {
        var users = await _context.Users.AsNoTracking().ToListAsync();
        var list = new List<UserActivityReportItemDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var customers = await _context.Customers.CountAsync(c => c.AssignedToId == user.Id);
            var leads = await _context.Leads.CountAsync(l => l.AssignedToId == user.Id);
            var opps = await _context.Opportunities.CountAsync(o => o.AssignedToId == user.Id);
            var followUpsDone = await _context.FollowUps.CountAsync(f => f.AssignedToId == user.Id && f.Status == FollowUpStatus.Completed);
            var activities = await _context.Activities.CountAsync(a => a.AssignedToId == user.Id);

            list.Add(new UserActivityReportItemDto
            {
                UserId = user.Id,
                UserName = user.FullName,
                Role = roles.FirstOrDefault() ?? "None",
                CustomersAssigned = customers,
                LeadsAssigned = leads,
                OpportunitiesAssigned = opps,
                FollowUpsCompleted = followUpsDone,
                ActivitiesLogged = activities
            });
        }

        return list;
    }

    public async Task<List<AuditLog>> GetAuditReportAsync(string? module = null, string? action = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(a => a.Module == module);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (startDate.HasValue)
        {
            var utcStart = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(a => a.CreatedDate >= utcStart);
        }

        if (endDate.HasValue)
        {
            var utcEnd = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(a => a.CreatedDate <= utcEnd);
        }

        return await query.OrderByDescending(a => a.CreatedDate).Take(500).ToListAsync();
    }

    public byte[] ExportToCsv<T>(IEnumerable<T> data, Func<T, string[]> rowMapper, string[] headers)
    {
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        foreach (var item in data)
        {
            var fields = rowMapper(item);
            sb.AppendLine(string.Join(",", fields.Select(EscapeCsvField)));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";

        var val = field;

        // Protection against CSV Formula Injection:
        // Characters '=', '+', '-', '@', '\t', '\r' can trigger formula execution in spreadsheet software
        if (val.StartsWith('=') || val.StartsWith('+') || val.StartsWith('-') || val.StartsWith('@'))
        {
            val = "'" + val;
        }

        if (val.Contains('"'))
        {
            val = val.Replace("\"", "\"\"");
        }

        return $"\"{val}\"";
    }
}
