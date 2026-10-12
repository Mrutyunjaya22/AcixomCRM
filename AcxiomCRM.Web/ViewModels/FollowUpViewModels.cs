using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.ViewModels;

public class FollowUpListViewModel
{
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<FollowUp> OverdueFollowUps { get; set; } = new();

    public FollowUpStatus? Status { get; set; }
    public FollowUpType? Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AssignedToId { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public SelectList? SalesUsersList { get; set; }
}

public class FollowUpCreateEditViewModel
{
    public int FollowUpId { get; set; }

    [Required(ErrorMessage = "Related type is required.")]
    [Display(Name = "Related To")]
    public FollowUpRelatedType RelatedType { get; set; } = FollowUpRelatedType.Customer;

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Opportunity")]
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Scheduled Date")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

    [Required(ErrorMessage = "Follow-up Type is required.")]
    [Display(Name = "Activity Type")]
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
    [Display(Name = "Subject / Agenda")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
    [Display(Name = "Remarks / Agenda Notes")]
    public string? Remarks { get; set; }

    [Display(Name = "Status")]
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [Display(Name = "Assigned User")]
    public string? AssignedToId { get; set; }

    public SelectList? CustomersList { get; set; }
    public SelectList? LeadsList { get; set; }
    public SelectList? OpportunitiesList { get; set; }
    public SelectList? SalesUsersList { get; set; }
}

public class FollowUpRescheduleViewModel
{
    public int FollowUpId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public DateTime CurrentDate { get; set; }

    [Required(ErrorMessage = "New follow-up date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "New Scheduled Date")]
    public DateTime NewDate { get; set; } = DateTime.UtcNow.AddDays(2);

    [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
    [Display(Name = "Reason for Rescheduling")]
    public string? Remarks { get; set; }
}
