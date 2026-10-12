using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;

namespace AcxiomCRM.Infrastructure.Services.Interfaces;

public interface ILeadService
{
    Task<(List<Lead> Leads, int TotalCount)> GetLeadsAsync(
        string? searchTerm = null,
        LeadStatus? status = null,
        LeadSource? source = null,
        string? assignedToId = null,
        string? sortBy = null,
        bool sortDescending = false,
        int page = 1,
        int pageSize = 10);

    Task<Lead?> GetLeadByIdAsync(int leadId);
    Task<Lead?> GetLeadWithDetailsAsync(int leadId);
    Task<Lead> CreateLeadAsync(Lead lead);
    Task<Lead> UpdateLeadAsync(Lead lead);
    Task<bool> DeleteOrDeactivateLeadAsync(int leadId);
    Task<(Customer Customer, Opportunity? Opportunity)> ConvertLeadAsync(int leadId, string? notes = null);
    Task<bool> CanAccessLeadAsync(int leadId);
    bool IsValidStatusTransition(LeadStatus currentStatus, LeadStatus newStatus);
}
