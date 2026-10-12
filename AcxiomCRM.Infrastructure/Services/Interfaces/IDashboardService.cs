using AcxiomCRM.Infrastructure.DTOs;

namespace AcxiomCRM.Infrastructure.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardMetricsAsync(string? dateFilter = null, DateTime? customStart = null, DateTime? customEnd = null);
}
