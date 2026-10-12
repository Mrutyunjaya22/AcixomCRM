using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;

namespace AcxiomCRM.Infrastructure.Services.Interfaces;

public interface IOpportunityService
{
    Task<(List<Opportunity> Opportunities, int TotalCount)> GetOpportunitiesAsync(
        string? searchTerm = null,
        OpportunityStage? stage = null,
        OpportunityStatus? status = null,
        string? assignedToId = null,
        int? customerId = null,
        string? sortBy = null,
        bool sortDescending = false,
        int page = 1,
        int pageSize = 10);

    Task<Opportunity?> GetOpportunityByIdAsync(int opportunityId);
    Task<Opportunity?> GetOpportunityWithDetailsAsync(int opportunityId);
    Task<Opportunity> CreateOpportunityAsync(Opportunity opportunity);
    Task<Opportunity> UpdateOpportunityAsync(Opportunity opportunity);
    Task<Opportunity> UpdateStageAsync(int opportunityId, OpportunityStage newStage);
    Task<bool> DeleteOrDeactivateOpportunityAsync(int opportunityId);
    Task<bool> CanAccessOpportunityAsync(int opportunityId);
}
