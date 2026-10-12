using AcxiomCRM.Core.Enums;

namespace AcxiomCRM.Core.Entities;

public class Opportunity
{
    public int OpportunityId { get; set; }
    public string OpportunityCode { get; set; } = string.Empty;
    public string OpportunityName { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public decimal Amount { get; set; }
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;
    public int Probability { get; set; } = 10;
    public DateTime ExpectedCloseDate { get; set; }
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public string? Notes { get; set; }

    public string? AssignedToId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }

    public decimal WeightedValue => Math.Round(Amount * Probability / 100m, 2);

    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
