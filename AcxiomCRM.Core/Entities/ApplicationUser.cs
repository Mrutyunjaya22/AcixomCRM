using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

    public ICollection<Customer> AssignedCustomers { get; set; } = new List<Customer>();
    public ICollection<Customer> CreatedCustomers { get; set; } = new List<Customer>();
    public ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
    public ICollection<Opportunity> AssignedOpportunities { get; set; } = new List<Opportunity>();
    public ICollection<FollowUp> AssignedFollowUps { get; set; } = new List<FollowUp>();
    public ICollection<Activity> AssignedActivities { get; set; } = new List<Activity>();
}
