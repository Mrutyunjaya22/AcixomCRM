using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.ViewModels;

public class CustomerListViewModel
{
    public List<Customer> Customers { get; set; } = new();
    public string? SearchTerm { get; set; }
    public CustomerStatus? Status { get; set; }
    public string? AssignedToId { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public SelectList? SalesUsersList { get; set; }
}

public class CustomerCreateEditViewModel
{
    public int CustomerId { get; set; }

    [Display(Name = "Customer Code")]
    public string? CustomerCode { get; set; }

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

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

    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    [Display(Name = "Street Address")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "City cannot exceed 50 characters.")]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(50, ErrorMessage = "State cannot exceed 50 characters.")]
    [Display(Name = "State / Province")]
    public string? State { get; set; }

    [Display(Name = "Status")]
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes / Remarks")]
    public string? Notes { get; set; }

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedToId { get; set; }

    public SelectList? SalesUsersList { get; set; }
}

public class CustomerDetailsViewModel
{
    public Customer Customer { get; set; } = null!;
    public bool CanEdit { get; set; }
}
