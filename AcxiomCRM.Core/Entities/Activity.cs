using AcxiomCRM.Core.Enums;

namespace AcxiomCRM.Core.Entities;

public class Activity
{
    public int ActivityId { get; set; }
    public ActivityType ActivityType { get; set; } = ActivityType.Call;
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActivityDate { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Planned;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public string? AssignedToId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
