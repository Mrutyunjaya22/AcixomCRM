using AcxiomCRM.Core.Constants;
using AcxiomCRM.Infrastructure.Services.Interfaces;

namespace AcxiomCRM.Tests.TestHelpers;

public class MockCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; } = "test-user-id";
    public string? UserName { get; set; } = "testuser@acxiomcrm.local";
    public string? Email { get; set; } = "testuser@acxiomcrm.local";
    public string? Role { get; set; } = Roles.Admin;
    public string? PrimaryRole => Role;
    public string? IpAddress { get; set; } = "127.0.0.1";
    public bool IsAuthenticated { get; set; } = true;

    public bool IsAdmin => Role == Roles.Admin;
    public bool IsManager => Role == Roles.Manager;
    public bool IsSalesExecutive => Role == Roles.SalesExecutive;

    public static MockCurrentUserService CreateAdmin(string id = "admin-1") => new()
    {
        UserId = id,
        UserName = "admin@acxiomcrm.local",
        Email = "admin@acxiomcrm.local",
        Role = Roles.Admin
    };

    public static MockCurrentUserService CreateManager(string id = "manager-1") => new()
    {
        UserId = id,
        UserName = "manager@acxiomcrm.local",
        Email = "manager@acxiomcrm.local",
        Role = Roles.Manager
    };

    public static MockCurrentUserService CreateSalesExecutive(string id = "sales-1") => new()
    {
        UserId = id,
        UserName = "sales1@acxiomcrm.local",
        Email = "sales1@acxiomcrm.local",
        Role = Roles.SalesExecutive
    };
}
