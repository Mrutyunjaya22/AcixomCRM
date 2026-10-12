using AcxiomCRM.Core.Entities;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AcxiomCRM.Infrastructure.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string module,
        string entityName,
        string? recordId = null,
        string? oldValue = null,
        string? newValue = null,
        string result = "Success",
        string? details = null,
        string? overrideUserId = null,
        string? overrideUserName = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = overrideUserId ?? _currentUserService.UserId,
                UserName = overrideUserName ?? _currentUserService.UserName ?? "System",
                Action = action,
                Module = module,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                Result = result,
                IpAddress = _currentUserService.IpAddress,
                CreatedDate = DateTime.UtcNow,
                Details = details
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Centralized logger ensures audit logging failure does not crash primary transaction unless critical,
            // while preserving structured error log
            _logger.LogError(ex, "Failed to write audit log for action: {Action}, module: {Module}", action, module);
        }
    }

    public async Task<(List<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        string? userId = null,
        string? module = null,
        string? action = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

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

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<List<string>> GetDistinctModulesAsync()
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.Module)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctActionsAsync()
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }
}
