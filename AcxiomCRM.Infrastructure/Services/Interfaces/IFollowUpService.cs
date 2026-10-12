using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;

namespace AcxiomCRM.Infrastructure.Services.Interfaces;

public interface IFollowUpService
{
    Task<(List<FollowUp> FollowUps, int TotalCount)> GetFollowUpsAsync(
        FollowUpStatus? status = null,
        FollowUpType? type = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? assignedToId = null,
        int? customerId = null,
        int? leadId = null,
        int? opportunityId = null,
        int page = 1,
        int pageSize = 10);

    Task<List<FollowUp>> GetUpcomingFollowUpsAsync(int limit = 5);
    Task<List<FollowUp>> GetOverdueFollowUpsAsync(int limit = 5);
    Task<FollowUp?> GetFollowUpByIdAsync(int followUpId);
    Task<FollowUp> CreateFollowUpAsync(FollowUp followUp);
    Task<FollowUp> UpdateFollowUpAsync(FollowUp followUp);
    Task<FollowUp> CompleteFollowUpAsync(int followUpId, string? remarks = null);
    Task<FollowUp> RescheduleFollowUpAsync(int followUpId, DateTime newDate, string? remarks = null);
    Task<FollowUp> CancelFollowUpAsync(int followUpId, string? reason = null);
    Task<bool> CanAccessFollowUpAsync(int followUpId);
}
