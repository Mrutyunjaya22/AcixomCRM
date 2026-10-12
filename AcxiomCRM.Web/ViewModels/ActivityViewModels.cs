using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.ViewModels;

public class ActivityListViewModel
{
    public List<Activity> Activities { get; set; } = new();
    public ActivityType? Type { get; set; }
    public ActivityStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AssignedToId { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public SelectList? SalesUsersList { get; set; }
}

public class ActivityCreateEditViewModel
{
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Activity Type is required.")]
    [Display(Name = "Activity Type")]
    public ActivityType ActivityType { get; set; } = ActivityType.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
    [Display(Name = "Subject / Headline")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Notes / Meeting Minutes")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Activity Date")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Status")]
    public ActivityStatus Status { get; set; } = ActivityStatus.Completed;

    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Assigned User")]
    public string? AssignedToId { get; set; }

    public SelectList? CustomersList { get; set; }
    public SelectList? LeadsList { get; set; }
    public SelectList? SalesUsersList { get; set; }
}
