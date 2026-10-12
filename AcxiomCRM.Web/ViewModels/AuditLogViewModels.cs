using AcxiomCRM.Core.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.ViewModels;

public class AuditLogListViewModel
{
    public List<AuditLog> Logs { get; set; } = new();
    public string? SelectedUserId { get; set; }
    public string? SelectedModule { get; set; }
    public string? SelectedAction { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public SelectList? ModulesList { get; set; }
    public SelectList? ActionsList { get; set; }
    public SelectList? UsersList { get; set; }
}
