using AcxiomCRM.Core.Enums;

namespace AcxiomCRM.Core.Entities;

public class FollowUp
{
    public int FollowUpId { get; set; }
    public FollowUpRelatedType RelatedType { get; set; } = FollowUpRelatedType.Customer;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }

    public DateTime FollowUpDate { get; set; }
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;
    public string Subject { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? AssignedToId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }
}
