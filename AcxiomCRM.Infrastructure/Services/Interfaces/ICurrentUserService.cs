namespace AcxiomCRM.Infrastructure.Services.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsManager { get; }
    bool IsSalesExecutive { get; }
    string? PrimaryRole { get; }
    string? IpAddress { get; }
}
