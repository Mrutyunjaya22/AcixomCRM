using AcxiomCRM.Core.Entities;

namespace AcxiomCRM.Infrastructure.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        string action,
        string module,
        string entityName,
        string? recordId = null,
        string? oldValue = null,
        string? newValue = null,
        string result = "Success",
        string? details = null,
        string? overrideUserId = null,
        string? overrideUserName = null);

    Task<(List<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        string? userId = null,
        string? module = null,
        string? action = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20);

    Task<List<string>> GetDistinctModulesAsync();
    Task<List<string>> GetDistinctActionsAsync();
}
