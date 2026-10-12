namespace AcxiomCRM.Core.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "Sales Executive";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Manager, SalesExecutive };
}
