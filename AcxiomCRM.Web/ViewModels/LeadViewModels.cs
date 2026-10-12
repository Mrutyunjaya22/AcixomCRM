using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.ViewModels;

public class LeadListViewModel
{
    public List<Lead> Leads { get; set; } = new();
    public string? SearchTerm { get; set; }
    public LeadStatus? Status { get; set; }
    public LeadSource? Source { get; set; }
    public string? AssignedToId { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public SelectList? SalesUsersList { get; set; }
}

public class LeadCreateEditViewModel
{
    public int LeadId { get; set; }

    [Display(Name = "Lead Code")]
    public string? LeadCode { get; set; }

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name / Contact Person")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number (7-20 digits/symbols).")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Company Name cannot exceed 100 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Display(Name = "Lead Source")]
    public LeadSource Source { get; set; } = LeadSource.Website;

    [Display(Name = "Status")]
    public LeadStatus Status { get; set; } = LeadStatus.New;

    [Display(Name = "Priority")]
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 100000000, ErrorMessage = "Expected Value must be 0 or greater.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Expected Deal Value ($)")]
    public decimal ExpectedValue { get; set; } = 0.00m;

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes / Discovery Details")]
    public string? Notes { get; set; }

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedToId { get; set; }

    public SelectList? SalesUsersList { get; set; }
}

public class LeadDetailsViewModel
{
    public Lead Lead { get; set; } = null!;
    public bool CanEdit { get; set; }
    public bool CanConvert => Lead.Status != LeadStatus.Converted && Lead.Status != LeadStatus.Lost;
}

public class LeadConvertViewModel
{
    public int LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public decimal ExpectedValue { get; set; }

    [Display(Name = "Conversion Notes")]
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}
