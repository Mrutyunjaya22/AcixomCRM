using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Core.Enums;

namespace AcxiomCRM.Web.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new() };
}

public class AuthLoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public CustomerStatus Status { get; set; }
    public string? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CustomerCreateDto
{
    [Required]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public string? AssignedToId { get; set; }
    public string? Notes { get; set; }
}

public class CustomerUpdateDto : CustomerCreateDto
{
}

public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public LeadSource Source { get; set; }
    public LeadStatus Status { get; set; }
    public LeadPriority Priority { get; set; }
    public decimal ExpectedValue { get; set; }
    public string? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public int? ConvertedCustomerId { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class LeadCreateDto
{
    [Required]
    [StringLength(100)]
    public string LeadName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public LeadSource Source { get; set; } = LeadSource.Website;
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 1000000000)]
    public decimal ExpectedValue { get; set; } = 0.00m;
    public string? AssignedToId { get; set; }
    public string? Notes { get; set; }
}

public class LeadUpdateDto : LeadCreateDto
{
}

public class OpportunityDto
{
    public int OpportunityId { get; set; }
    public string OpportunityCode { get; set; } = string.Empty;
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public OpportunityStage Stage { get; set; }
    public int Probability { get; set; }
    public decimal WeightedValue { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public OpportunityStatus Status { get; set; }
    public string? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class OpportunityCreateDto
{
    [Required]
    [StringLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required]
    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 20;

    [Required]
    public DateTime ExpectedCloseDate { get; set; }

    public string? AssignedToId { get; set; }
    public string? Notes { get; set; }
}

public class OpportunityUpdateDto : OpportunityCreateDto
{
}

public class FollowUpDto
{
    public int FollowUpId { get; set; }
    public FollowUpRelatedType RelatedType { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int? OpportunityId { get; set; }
    public string? OpportunityName { get; set; }
    public DateTime FollowUpDate { get; set; }
    public FollowUpType FollowUpType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public FollowUpStatus Status { get; set; }
    public string? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class FollowUpCreateDto
{
    [Required]
    public FollowUpRelatedType RelatedType { get; set; } = FollowUpRelatedType.Customer;

    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }

    [Required]
    public DateTime FollowUpDate { get; set; }

    [Required]
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    [Required]
    [StringLength(150)]
    public string Subject { get; set; } = string.Empty;

    public string? Remarks { get; set; }
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;
    public string? AssignedToId { get; set; }
}
