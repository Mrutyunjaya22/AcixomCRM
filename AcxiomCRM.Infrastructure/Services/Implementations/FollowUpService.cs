using System.Text.Json;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Infrastructure.Services.Implementations;

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLog;

    public FollowUpService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditLogService auditLog)
    {
        _context = context;
        _currentUser = currentUser;
        _auditLog = auditLog;
    }

    private IQueryable<FollowUp> ApplyScope(IQueryable<FollowUp> query)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            return query;
        }

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            return query.Where(f => f.AssignedToId == _currentUser.UserId);
        }

        return query.Where(_ => false);
    }

    public async Task<bool> CanAccessFollowUpAsync(int followUpId)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager) return true;
        if (string.IsNullOrEmpty(_currentUser.UserId)) return false;

        return await _context.FollowUps
            .AsNoTracking()
            .AnyAsync(f => f.FollowUpId == followUpId && f.AssignedToId == _currentUser.UserId);
    }

    public async Task<(List<FollowUp> FollowUps, int TotalCount)> GetFollowUpsAsync(
        FollowUpStatus? status = null,
        FollowUpType? type = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? assignedToId = null,
        int? customerId = null,
        int? leadId = null,
        int? opportunityId = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyScope(query);

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(f => f.FollowUpType == type.Value);
        }

        if (startDate.HasValue)
        {
            var utcStart = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(f => f.FollowUpDate >= utcStart);
        }

        if (endDate.HasValue)
        {
            var utcEnd = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(f => f.FollowUpDate <= utcEnd);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(f => f.AssignedToId == assignedToId);
        }

        if (customerId.HasValue)
        {
            query = query.Where(f => f.CustomerId == customerId.Value);
        }

        if (leadId.HasValue)
        {
            query = query.Where(f => f.LeadId == leadId.Value);
        }

        if (opportunityId.HasValue)
        {
            query = query.Where(f => f.OpportunityId == opportunityId.Value);
        }

        var totalCount = await query.CountAsync();

        var followUps = await query
            .OrderBy(f => f.FollowUpDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (followUps, totalCount);
    }

    public async Task<List<FollowUp>> GetUpcomingFollowUpsAsync(int limit = 5)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedTo)
            .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate >= DateTime.UtcNow.Date)
            .OrderBy(f => f.FollowUpDate)
            .Take(limit)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyScope(query);
        return await query.ToListAsync();
    }

    public async Task<List<FollowUp>> GetOverdueFollowUpsAsync(int limit = 5)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedTo)
            .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < DateTime.UtcNow.Date)
            .OrderBy(f => f.FollowUpDate)
            .Take(limit)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyScope(query);
        return await query.ToListAsync();
    }

    public async Task<FollowUp?> GetFollowUpByIdAsync(int followUpId)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedTo)
            .AsQueryable();

        query = ApplyScope(query);
        return await query.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
    }

    public async Task<FollowUp> CreateFollowUpAsync(FollowUp followUp)
    {
        if (followUp.Status == FollowUpStatus.Planned && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            throw new DomainException("Follow-up date cannot be earlier than today.");
        }

        if (followUp.CustomerId.HasValue && !await _context.Customers.AnyAsync(c => c.CustomerId == followUp.CustomerId.Value))
        {
            throw new DomainException("Referenced customer does not exist.");
        }

        if (followUp.LeadId.HasValue && !await _context.Leads.AnyAsync(l => l.LeadId == followUp.LeadId.Value))
        {
            throw new DomainException("Referenced lead does not exist.");
        }

        if (followUp.OpportunityId.HasValue && !await _context.Opportunities.AnyAsync(o => o.OpportunityId == followUp.OpportunityId.Value))
        {
            throw new DomainException("Referenced opportunity does not exist.");
        }

        followUp.Subject = followUp.Subject.Trim();
        followUp.CreatedDate = DateTime.UtcNow;

        if (_currentUser.IsSalesExecutive && string.IsNullOrWhiteSpace(followUp.AssignedToId))
        {
            followUp.AssignedToId = _currentUser.UserId;
        }

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Create",
            module: "FollowUp",
            entityName: nameof(FollowUp),
            recordId: followUp.FollowUpId.ToString(),
            oldValue: null,
            newValue: JsonSerializer.Serialize(new { followUp.Subject, followUp.FollowUpDate, followUp.FollowUpType, followUp.Status }),
            details: $"Follow-up '{followUp.Subject}' scheduled for {followUp.FollowUpDate:d}."
        );

        return followUp;
    }

    public async Task<FollowUp> UpdateFollowUpAsync(FollowUp followUp)
    {
        var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUp.FollowUpId);
        if (existing == null)
        {
            throw new DomainException($"Follow-up with ID {followUp.FollowUpId} was not found.");
        }

        if (!await CanAccessFollowUpAsync(existing.FollowUpId))
        {
            throw new UnauthorizedAccessException("You are not authorized to update this follow-up.");
        }

        if (followUp.Status == FollowUpStatus.Planned && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            throw new DomainException("Follow-up date cannot be earlier than today.");
        }

        var oldSnapshot = JsonSerializer.Serialize(new { existing.Subject, existing.FollowUpDate, existing.FollowUpType, existing.Status, existing.Remarks });

        existing.Subject = followUp.Subject.Trim();
        existing.FollowUpDate = followUp.FollowUpDate;
        existing.FollowUpType = followUp.FollowUpType;
        existing.Status = followUp.Status;
        existing.Remarks = followUp.Remarks?.Trim();

        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            existing.AssignedToId = followUp.AssignedToId;
        }

        await _context.SaveChangesAsync();

        var newSnapshot = JsonSerializer.Serialize(new { existing.Subject, existing.FollowUpDate, existing.FollowUpType, existing.Status, existing.Remarks });

        await _auditLog.LogAsync(
            action: "Update",
            module: "FollowUp",
            entityName: nameof(FollowUp),
            recordId: existing.FollowUpId.ToString(),
            oldValue: oldSnapshot,
            newValue: newSnapshot,
            details: $"Follow-up '{existing.Subject}' updated."
        );

        return existing;
    }

    public async Task<FollowUp> CompleteFollowUpAsync(int followUpId, string? remarks = null)
    {
        var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
        if (existing == null)
        {
            throw new DomainException($"Follow-up with ID {followUpId} was not found.");
        }

        if (!await CanAccessFollowUpAsync(existing.FollowUpId))
        {
            throw new UnauthorizedAccessException("You are not authorized to complete this follow-up.");
        }

        var oldStatus = existing.Status;
        existing.Status = FollowUpStatus.Completed;
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            existing.Remarks = (existing.Remarks != null ? existing.Remarks + " | Completed: " : "Completed: ") + remarks.Trim();
        }

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Complete",
            module: "FollowUp",
            entityName: nameof(FollowUp),
            recordId: existing.FollowUpId.ToString(),
            oldValue: oldStatus.ToString(),
            newValue: FollowUpStatus.Completed.ToString(),
            details: $"Follow-up '{existing.Subject}' marked as Completed."
        );

        return existing;
    }

    public async Task<FollowUp> RescheduleFollowUpAsync(int followUpId, DateTime newDate, string? remarks = null)
    {
        var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
        if (existing == null)
        {
            throw new DomainException($"Follow-up with ID {followUpId} was not found.");
        }

        if (!await CanAccessFollowUpAsync(existing.FollowUpId))
        {
            throw new UnauthorizedAccessException("You are not authorized to reschedule this follow-up.");
        }

        if (newDate.Date < DateTime.UtcNow.Date)
        {
            throw new DomainException("Follow-up date cannot be earlier than today.");
        }

        var oldDate = existing.FollowUpDate;
        existing.FollowUpDate = newDate;
        existing.Status = FollowUpStatus.Planned;
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            existing.Remarks = (existing.Remarks != null ? existing.Remarks + " | Rescheduled: " : "Rescheduled: ") + remarks.Trim();
        }

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Reschedule",
            module: "FollowUp",
            entityName: nameof(FollowUp),
            recordId: existing.FollowUpId.ToString(),
            oldValue: oldDate.ToString("o"),
            newValue: newDate.ToString("o"),
            details: $"Follow-up '{existing.Subject}' rescheduled from {oldDate:d} to {newDate:d}."
        );

        return existing;
    }

    public async Task<FollowUp> CancelFollowUpAsync(int followUpId, string? reason = null)
    {
        var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
        if (existing == null)
        {
            throw new DomainException($"Follow-up with ID {followUpId} was not found.");
        }

        if (!await CanAccessFollowUpAsync(existing.FollowUpId))
        {
            throw new UnauthorizedAccessException("You are not authorized to cancel this follow-up.");
        }

        var oldStatus = existing.Status;
        existing.Status = FollowUpStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            existing.Remarks = (existing.Remarks != null ? existing.Remarks + " | Cancelled: " : "Cancelled: ") + reason.Trim();
        }

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Cancel",
            module: "FollowUp",
            entityName: nameof(FollowUp),
            recordId: existing.FollowUpId.ToString(),
            oldValue: oldStatus.ToString(),
            newValue: FollowUpStatus.Cancelled.ToString(),
            details: $"Follow-up '{existing.Subject}' was cancelled."
        );

        return existing;
    }
}
