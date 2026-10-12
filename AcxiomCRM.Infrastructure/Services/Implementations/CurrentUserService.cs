using System.Security.Claims;
using AcxiomCRM.Core.Constants;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AcxiomCRM.Infrastructure.Services.Implementations;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => User?.Identity?.Name;

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => User?.IsInRole(Roles.Admin) ?? false;

    public bool IsManager => User?.IsInRole(Roles.Manager) ?? false;

    public bool IsSalesExecutive => User?.IsInRole(Roles.SalesExecutive) ?? false;

    public string? PrimaryRole
    {
        get
        {
            if (IsAdmin) return Roles.Admin;
            if (IsManager) return Roles.Manager;
            if (IsSalesExecutive) return Roles.SalesExecutive;
            return User?.FindFirstValue(ClaimTypes.Role);
        }
    }

    public string? IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
            {
                return forwarded.ToString().Split(',').FirstOrDefault()?.Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
