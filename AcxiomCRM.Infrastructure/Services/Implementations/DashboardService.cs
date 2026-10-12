using System.Globalization;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.DTOs;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Infrastructure.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DashboardDto> GetDashboardMetricsAsync(string? dateFilter = null, DateTime? customStart = null, DateTime? customEnd = null)
    {
        var dto = new DashboardDto { DateFilter = dateFilter ?? "All" };

        var customersQuery = _context.Customers.AsNoTracking().AsQueryable();
        var leadsQuery = _context.Leads.AsNoTracking().AsQueryable();
        var oppsQuery = _context.Opportunities.AsNoTracking().AsQueryable();
        var followUpsQuery = _context.FollowUps.AsNoTracking().AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            customersQuery = customersQuery.Where(c => c.AssignedToId == _currentUser.UserId);
            leadsQuery = leadsQuery.Where(l => l.AssignedToId == _currentUser.UserId);
            oppsQuery = oppsQuery.Where(o => o.AssignedToId == _currentUser.UserId);
            followUpsQuery = followUpsQuery.Where(f => f.AssignedToId == _currentUser.UserId);
        }

        DateTime? filterStart = null;
        DateTime? filterEnd = null;
        var now = DateTime.UtcNow;

        switch (dateFilter?.ToLower())
        {
            case "today":
                filterStart = now.Date;
                filterEnd = now.Date.AddDays(1).AddTicks(-1);
                break;
            case "thisweek":
                var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                filterStart = now.Date.AddDays(-1 * diff);
                filterEnd = now.Date.AddDays(1).AddTicks(-1);
                break;
            case "thismonth":
                filterStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                filterEnd = filterStart.Value.AddMonths(1).AddTicks(-1);
                break;
            case "custom":
                if (customStart.HasValue) filterStart = DateTime.SpecifyKind(customStart.Value.Date, DateTimeKind.Utc);
                if (customEnd.HasValue) filterEnd = DateTime.SpecifyKind(customEnd.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                break;
        }

        if (filterStart.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedDate >= filterStart.Value);
            leadsQuery = leadsQuery.Where(l => l.CreatedDate >= filterStart.Value);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate >= filterStart.Value);
        }

        if (filterEnd.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedDate <= filterEnd.Value);
            leadsQuery = leadsQuery.Where(l => l.CreatedDate <= filterEnd.Value);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate <= filterEnd.Value);
        }

        dto.TotalCustomers = await customersQuery.CountAsync();
        dto.TotalLeads = await leadsQuery.CountAsync();
        dto.OpenLeads = await leadsQuery.CountAsync(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost);

        dto.TotalOpportunities = await oppsQuery.CountAsync();
        dto.OpenOpportunities = await oppsQuery.CountAsync(o => o.Status == OpportunityStatus.Open);
        dto.WonOpportunities = await oppsQuery.CountAsync(o => o.Status == OpportunityStatus.Won);
        dto.LostOpportunities = await oppsQuery.CountAsync(o => o.Status == OpportunityStatus.Lost);

        var openOpps = await oppsQuery
            .Where(o => o.Status == OpportunityStatus.Open)
            .Select(o => new { o.Amount, o.Probability })
            .ToListAsync();

        dto.TotalPipelineValue = openOpps.Sum(o => o.Amount);
        dto.WeightedPipelineValue = openOpps.Sum(o => Math.Round(o.Amount * o.Probability / 100m, 2));

        dto.PendingFollowUps = await followUpsQuery.CountAsync(f => f.Status == FollowUpStatus.Planned);
        dto.OverdueFollowUps = await followUpsQuery.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < now.Date);

        var leadStatuses = new[]
        {
            LeadStatus.New,
            LeadStatus.Contacted,
            LeadStatus.Qualified,
            LeadStatus.Unqualified,
            LeadStatus.Converted,
            LeadStatus.Lost
        };

        var leadGroupings = await leadsQuery
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var status in leadStatuses)
        {
            dto.LeadStatusLabels.Add(status.ToString());
            dto.LeadStatusCounts.Add(leadGroupings.FirstOrDefault(g => g.Status == status)?.Count ?? 0);
        }

        var stages = new[]
        {
            OpportunityStage.Qualification,
            OpportunityStage.Proposal,
            OpportunityStage.Negotiation,
            OpportunityStage.Won,
            OpportunityStage.Lost
        };

        var oppItems = await oppsQuery
            .Select(o => new { o.Stage, o.Amount })
            .ToListAsync();

        var oppGroupings = oppItems
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Count(), TotalAmount = g.Sum(x => x.Amount) })
            .ToList();

        foreach (var stage in stages)
        {
            dto.OpportunityStageLabels.Add(stage.ToString());
            var matched = oppGroupings.FirstOrDefault(g => g.Stage == stage);
            dto.OpportunityStageCounts.Add(matched?.Count ?? 0);
            dto.OpportunityStageAmounts.Add(matched?.TotalAmount ?? 0m);
        }

        var months = Enumerable.Range(0, 6)
            .Select(i => now.AddMonths(-5 + i))
            .ToList();

        var sixMonthsAgo = new DateTime(months.First().Year, months.First().Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var wonOpps = await _context.Opportunities
            .AsNoTracking()
            .Where(o => o.Status == OpportunityStatus.Won && o.CreatedDate >= sixMonthsAgo)
            .Select(o => new { o.Amount, o.CreatedDate, o.AssignedToId })
            .ToListAsync();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            wonOpps = wonOpps.Where(o => o.AssignedToId == _currentUser.UserId).ToList();
        }

        foreach (var m in months)
        {
            dto.MonthlySalesLabels.Add(m.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            var sumForMonth = wonOpps
                .Where(o => o.CreatedDate.Year == m.Year && o.CreatedDate.Month == m.Month)
                .Sum(o => o.Amount);
            dto.MonthlySalesValues.Add(sumForMonth);
        }

        return dto;
    }
}
