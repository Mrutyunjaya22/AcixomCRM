using System.Text.Json;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Infrastructure.Services.Implementations;

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLog;

    public ActivityService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditLogService auditLog)
    {
        _context = context;
        _currentUser = currentUser;
        _auditLog = auditLog;
    }

    private IQueryable<Activity> ApplyScope(IQueryable<Activity> query)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            return query;
        }

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            return query.Where(a => a.AssignedToId == _currentUser.UserId);
        }

        return query.Where(_ => false);
    }

    public async Task<(List<Activity> Activities, int TotalCount)> GetActivitiesAsync(
        ActivityType? type = null,
        ActivityStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? assignedToId = null,
        int? customerId = null,
        int? leadId = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyScope(query);

        if (type.HasValue)
        {
            query = query.Where(a => a.ActivityType == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            var utcStart = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(a => a.ActivityDate >= utcStart);
        }

        if (endDate.HasValue)
        {
            var utcEnd = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(a => a.ActivityDate <= utcEnd);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(a => a.AssignedToId == assignedToId);
        }

        if (customerId.HasValue)
        {
            query = query.Where(a => a.CustomerId == customerId.Value);
        }

        if (leadId.HasValue)
        {
            query = query.Where(a => a.LeadId == leadId.Value);
        }

        var totalCount = await query.CountAsync();

        var activities = await query
            .OrderByDescending(a => a.ActivityDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (activities, totalCount);
    }

    public async Task<Activity?> GetActivityByIdAsync(int activityId)
    {
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedTo)
            .AsQueryable();

        query = ApplyScope(query);
        return await query.FirstOrDefaultAsync(a => a.ActivityId == activityId);
    }

    public async Task<Activity> CreateActivityAsync(Activity activity)
    {
        activity.Subject = activity.Subject.Trim();
        activity.CreatedDate = DateTime.UtcNow;

        if (_currentUser.IsSalesExecutive && string.IsNullOrWhiteSpace(activity.AssignedToId))
        {
            activity.AssignedToId = _currentUser.UserId;
        }

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Create",
            module: "Activity",
            entityName: nameof(Activity),
            recordId: activity.ActivityId.ToString(),
            oldValue: null,
            newValue: JsonSerializer.Serialize(new { activity.Subject, activity.ActivityType, activity.ActivityDate, activity.Status }),
            details: $"Activity '{activity.Subject}' ({activity.ActivityType}) logged."
        );

        return activity;
    }

    public async Task<Activity> UpdateActivityAsync(Activity activity)
    {
        var existing = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activity.ActivityId);
        if (existing == null)
        {
            throw new DomainException($"Activity with ID {activity.ActivityId} was not found.");
        }

        var oldSnapshot = JsonSerializer.Serialize(new { existing.Subject, existing.ActivityType, existing.ActivityDate, existing.Status });

        existing.Subject = activity.Subject.Trim();
        existing.Description = activity.Description?.Trim();
        existing.ActivityType = activity.ActivityType;
        existing.ActivityDate = activity.ActivityDate;
        existing.Status = activity.Status;

        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            existing.AssignedToId = activity.AssignedToId;
        }

        await _context.SaveChangesAsync();

        var newSnapshot = JsonSerializer.Serialize(new { existing.Subject, existing.ActivityType, existing.ActivityDate, existing.Status });

        await _auditLog.LogAsync(
            action: "Update",
            module: "Activity",
            entityName: nameof(Activity),
            recordId: existing.ActivityId.ToString(),
            oldValue: oldSnapshot,
            newValue: newSnapshot,
            details: $"Activity '{existing.Subject}' updated."
        );

        return existing;
    }

    public async Task<bool> DeleteActivityAsync(int activityId)
    {
        var existing = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activityId);
        if (existing == null) return false;

        _context.Activities.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Delete",
            module: "Activity",
            entityName: nameof(Activity),
            recordId: activityId.ToString(),
            oldValue: existing.Subject,
            newValue: null,
            details: $"Activity '{existing.Subject}' deleted."
        );

        return true;
    }
}
