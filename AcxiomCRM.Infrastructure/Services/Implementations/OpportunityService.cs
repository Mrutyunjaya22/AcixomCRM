using System.Text.Json;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Infrastructure.Services.Implementations;

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLog;
    private readonly ICustomerService _customerService;

    public OpportunityService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditLogService auditLog,
        ICustomerService customerService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditLog = auditLog;
        _customerService = customerService;
    }

    private IQueryable<Opportunity> ApplyScope(IQueryable<Opportunity> query)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            return query;
        }

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            return query.Where(o => o.AssignedToId == _currentUser.UserId);
        }

        return query.Where(_ => false);
    }

    public async Task<bool> CanAccessOpportunityAsync(int opportunityId)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager) return true;
        if (string.IsNullOrEmpty(_currentUser.UserId)) return false;

        return await _context.Opportunities
            .AsNoTracking()
            .AnyAsync(o => o.OpportunityId == opportunityId && o.AssignedToId == _currentUser.UserId);
    }

    private void ValidateOpportunity(Opportunity opportunity)
    {
        if (opportunity.Amount <= 0)
        {
            throw new DomainException("Opportunity Amount must be greater than 0.");
        }

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            throw new DomainException("Probability must be between 0 and 100.");
        }

        if (opportunity.Stage != OpportunityStage.Won && opportunity.Stage != OpportunityStage.Lost)
        {
            if (opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                throw new DomainException("Expected Close Date cannot be in the past.");
            }
        }
    }

    public async Task<(List<Opportunity> Opportunities, int TotalCount)> GetOpportunitiesAsync(
        string? searchTerm = null,
        OpportunityStage? stage = null,
        OpportunityStatus? status = null,
        string? assignedToId = null,
        int? customerId = null,
        string? sortBy = null,
        bool sortDescending = false,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyScope(query);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(o =>
                o.OpportunityName.ToLower().Contains(term) ||
                o.OpportunityCode.ToLower().Contains(term) ||
                o.Customer.CustomerName.ToLower().Contains(term));
        }

        if (stage.HasValue)
        {
            query = query.Where(o => o.Stage == stage.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(o => o.AssignedToId == assignedToId);
        }

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        query = (sortBy?.ToLower()) switch
        {
            "name" => sortDescending ? query.OrderByDescending(o => o.OpportunityName) : query.OrderBy(o => o.OpportunityName),
            "amount" => sortDescending ? query.OrderByDescending(o => o.Amount) : query.OrderBy(o => o.Amount),
            "probability" => sortDescending ? query.OrderByDescending(o => o.Probability) : query.OrderBy(o => o.Probability),
            "stage" => sortDescending ? query.OrderByDescending(o => o.Stage) : query.OrderBy(o => o.Stage),
            "closedate" => sortDescending ? query.OrderByDescending(o => o.ExpectedCloseDate) : query.OrderBy(o => o.ExpectedCloseDate),
            _ => sortDescending ? query.OrderBy(o => o.CreatedDate) : query.OrderByDescending(o => o.CreatedDate)
        };

        var totalCount = await query.CountAsync();

        var opportunities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (opportunities, totalCount);
    }

    public async Task<Opportunity?> GetOpportunityByIdAsync(int opportunityId)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedTo)
            .AsQueryable();

        query = ApplyScope(query);

        return await query.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);
    }

    public async Task<Opportunity?> GetOpportunityWithDetailsAsync(int opportunityId)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedTo)
            .Include(o => o.FollowUps.OrderByDescending(f => f.FollowUpDate))
            .AsSplitQuery()
            .AsQueryable();

        query = ApplyScope(query);

        return await query.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);
    }

    public async Task<Opportunity> CreateOpportunityAsync(Opportunity opportunity)
    {
        ValidateOpportunity(opportunity);

        if (!await _customerService.CanAccessCustomerAsync(opportunity.CustomerId))
        {
            throw new DomainException("Selected customer does not exist or is not accessible.");
        }

        if (opportunity.LeadId.HasValue && !await _context.Leads.AnyAsync(l => l.LeadId == opportunity.LeadId.Value))
        {
            throw new DomainException("Selected lead does not exist.");
        }

        if (string.IsNullOrWhiteSpace(opportunity.OpportunityCode))
        {
            var maxId = (await _context.Opportunities.MaxAsync(o => (int?)o.OpportunityId)) ?? 0;
            opportunity.OpportunityCode = $"OPP-{(maxId + 1):D5}";
        }

        if (opportunity.Stage == OpportunityStage.Won)
        {
            opportunity.Status = OpportunityStatus.Won;
            opportunity.Probability = 100;
        }
        else if (opportunity.Stage == OpportunityStage.Lost)
        {
            opportunity.Status = OpportunityStatus.Lost;
            opportunity.Probability = 0;
        }
        else
        {
            opportunity.Status = OpportunityStatus.Open;
        }

        opportunity.CreatedDate = DateTime.UtcNow;

        if (_currentUser.IsSalesExecutive && string.IsNullOrWhiteSpace(opportunity.AssignedToId))
        {
            opportunity.AssignedToId = _currentUser.UserId;
        }

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Create",
            module: "Opportunity",
            entityName: nameof(Opportunity),
            recordId: opportunity.OpportunityId.ToString(),
            oldValue: null,
            newValue: JsonSerializer.Serialize(new { opportunity.OpportunityCode, opportunity.OpportunityName, opportunity.Amount, opportunity.Stage, opportunity.Probability }),
            details: $"Opportunity {opportunity.OpportunityName} ({opportunity.OpportunityCode}) created with Amount: {opportunity.Amount:C}."
        );

        return opportunity;
    }

    public async Task<Opportunity> UpdateOpportunityAsync(Opportunity opportunity)
    {
        var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunity.OpportunityId);
        if (existing == null)
        {
            throw new DomainException($"Opportunity with ID {opportunity.OpportunityId} was not found.");
        }

        if (!await CanAccessOpportunityAsync(existing.OpportunityId))
        {
            throw new UnauthorizedAccessException("You are not authorized to update this opportunity.");
        }

        ValidateOpportunity(opportunity);

        var oldSnapshot = JsonSerializer.Serialize(new { existing.OpportunityName, existing.Amount, existing.Stage, existing.Probability, existing.ExpectedCloseDate, existing.Status, existing.AssignedToId });

        existing.OpportunityName = opportunity.OpportunityName.Trim();
        existing.Amount = opportunity.Amount;
        existing.Probability = opportunity.Probability;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Stage = opportunity.Stage;
        existing.Notes = opportunity.Notes?.Trim();
        existing.ModifiedDate = DateTime.UtcNow;

        if (opportunity.Stage == OpportunityStage.Won)
        {
            existing.Status = OpportunityStatus.Won;
            existing.Probability = 100;
        }
        else if (opportunity.Stage == OpportunityStage.Lost)
        {
            existing.Status = OpportunityStatus.Lost;
            existing.Probability = 0;
        }
        else
        {
            existing.Status = OpportunityStatus.Open;
        }

        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            existing.AssignedToId = opportunity.AssignedToId;
        }

        await _context.SaveChangesAsync();

        var newSnapshot = JsonSerializer.Serialize(new { existing.OpportunityName, existing.Amount, existing.Stage, existing.Probability, existing.ExpectedCloseDate, existing.Status, existing.AssignedToId });

        await _auditLog.LogAsync(
            action: "Update",
            module: "Opportunity",
            entityName: nameof(Opportunity),
            recordId: existing.OpportunityId.ToString(),
            oldValue: oldSnapshot,
            newValue: newSnapshot,
            details: $"Opportunity {existing.OpportunityName} ({existing.OpportunityCode}) updated. Stage: {existing.Stage}."
        );

        return existing;
    }

    public async Task<Opportunity> UpdateStageAsync(int opportunityId, OpportunityStage newStage)
    {
        var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);
        if (existing == null)
        {
            throw new DomainException($"Opportunity with ID {opportunityId} was not found.");
        }

        if (!await CanAccessOpportunityAsync(existing.OpportunityId))
        {
            throw new UnauthorizedAccessException("You are not authorized to update this opportunity stage.");
        }

        var oldStage = existing.Stage;
        existing.Stage = newStage;
        existing.ModifiedDate = DateTime.UtcNow;

        if (newStage == OpportunityStage.Won)
        {
            existing.Status = OpportunityStatus.Won;
            existing.Probability = 100;
        }
        else if (newStage == OpportunityStage.Lost)
        {
            existing.Status = OpportunityStatus.Lost;
            existing.Probability = 0;
        }
        else
        {
            existing.Status = OpportunityStatus.Open;
            if (newStage == OpportunityStage.Qualification) existing.Probability = 20;
            else if (newStage == OpportunityStage.Proposal) existing.Probability = 50;
            else if (newStage == OpportunityStage.Negotiation) existing.Probability = 75;
        }

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "StageChange",
            module: "Opportunity",
            entityName: nameof(Opportunity),
            recordId: existing.OpportunityId.ToString(),
            oldValue: oldStage.ToString(),
            newValue: newStage.ToString(),
            details: $"Opportunity {existing.OpportunityName} stage moved from {oldStage} to {newStage}."
        );

        return existing;
    }

    public async Task<bool> DeleteOrDeactivateOpportunityAsync(int opportunityId)
    {
        var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);
        if (existing == null) return false;

        if (!await CanAccessOpportunityAsync(opportunityId))
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this opportunity.");
        }

        existing.Status = OpportunityStatus.Lost;
        existing.Stage = OpportunityStage.Lost;
        existing.Probability = 0;
        existing.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Deactivate",
            module: "Opportunity",
            entityName: nameof(Opportunity),
            recordId: existing.OpportunityId.ToString(),
            oldValue: null,
            newValue: OpportunityStatus.Lost.ToString(),
            details: $"Opportunity {existing.OpportunityName} ({existing.OpportunityCode}) marked as Lost."
        );

        return true;
    }
}
