using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;

namespace AcxiomCRM.Infrastructure.Services.Interfaces;

public interface IActivityService
{
    Task<(List<Activity> Activities, int TotalCount)> GetActivitiesAsync(
        ActivityType? type = null,
        ActivityStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? assignedToId = null,
        int? customerId = null,
        int? leadId = null,
        int page = 1,
        int pageSize = 10);

    Task<Activity?> GetActivityByIdAsync(int activityId);
    Task<Activity> CreateActivityAsync(Activity activity);
    Task<Activity> UpdateActivityAsync(Activity activity);
    Task<bool> DeleteActivityAsync(int activityId);
}
