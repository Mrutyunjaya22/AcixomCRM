using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.ViewModels;

public class OpportunityListViewModel
{
    public List<Opportunity> Opportunities { get; set; } = new();
    public string? SearchTerm { get; set; }
    public OpportunityStage? Stage { get; set; }
    public OpportunityStatus? Status { get; set; }
    public string? AssignedToId { get; set; }
    public int? CustomerId { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public SelectList? SalesUsersList { get; set; }
}

public class OpportunityCreateEditViewModel
{
    public int OpportunityId { get; set; }

    [Display(Name = "Opportunity Code")]
    public string? OpportunityCode { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer selection is required.")]
    [Display(Name = "Related Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Deal Amount is required.")]
    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Amount ($)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    [Display(Name = "Pipeline Stage")]
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddMonths(1);

    [Display(Name = "Status")]
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes / Strategy")]
    public string? Notes { get; set; }

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedToId { get; set; }

    public SelectList? CustomersList { get; set; }
    public SelectList? LeadsList { get; set; }
    public SelectList? SalesUsersList { get; set; }
}

public class OpportunityDetailsViewModel
{
    public Opportunity Opportunity { get; set; } = null!;
    public bool CanEdit { get; set; }
}
