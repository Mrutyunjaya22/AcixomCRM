namespace AcxiomCRM.Core.Entities;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Result { get; set; } = "Success";
    public string? IpAddress { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; }
}
