using AcxiomCRM.Core.Constants;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Infrastructure.Services.Implementations;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditLogService _auditLog;

    public UserService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditLogService auditLog)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _auditLog = auditLog;
    }

    public async Task<List<UserDto>> GetUsersAsync(string? searchTerm = null, string? role = null, bool? isActive = null)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) || (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var users = await query.OrderByDescending(u => u.CreatedDate).ToListAsync();
        var dtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "None";

            if (!string.IsNullOrWhiteSpace(role) && !string.Equals(userRole, role, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

            dtos.Add(new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = userRole,
                IsActive = user.IsActive,
                IsLockedOut = isLocked,
                AccessFailedCount = user.AccessFailedCount,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            });
        }

        return dtos;
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? "None",
            IsActive = user.IsActive,
            IsLockedOut = isLocked,
            AccessFailedCount = user.AccessFailedCount,
            CreatedDate = user.CreatedDate,
            LastLoginDate = user.LastLoginDate
        };
    }

    public async Task<ApplicationUser> CreateUserAsync(string fullName, string email, string password, string role)
    {
        var cleanEmail = email.Trim().ToLower();
        var existing = await _userManager.FindByEmailAsync(cleanEmail);
        if (existing != null)
        {
            throw new DomainException($"User with email '{cleanEmail}' already exists.");
        }

        if (!Roles.All.Contains(role))
        {
            throw new DomainException($"Role '{role}' is not a recognized system role.");
        }

        var user = new ApplicationUser
        {
            UserName = cleanEmail,
            Email = cleanEmail,
            FullName = fullName.Trim(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new DomainException($"Failed to create user: {errors}");
        }

        await _userManager.AddToRoleAsync(user, role);

        await _auditLog.LogAsync(
            action: "Create",
            module: "User",
            entityName: nameof(ApplicationUser),
            recordId: user.Id,
            oldValue: null,
            newValue: $"Email: {user.Email}, Role: {role}",
            details: $"New user {user.FullName} ({user.Email}) created with role {role}."
        );

        return user;
    }

    public async Task<bool> UpdateUserRoleAsync(string userId, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new DomainException("User not found.");

        if (!Roles.All.Contains(newRole))
        {
            throw new DomainException($"Role '{newRole}' is invalid.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault();

        if (currentRole == newRole) return true;

        // Prevent stripping Admin role from the last active admin
        if (currentRole == Roles.Admin && newRole != Roles.Admin)
        {
            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            var activeAdminCount = admins.Count(a => a.IsActive);
            if (activeAdminCount <= 1)
            {
                throw new DomainException("Cannot remove Admin role from the only active Administrator.");
            }
        }

        if (currentRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        await _userManager.AddToRoleAsync(user, newRole);

        await _auditLog.LogAsync(
            action: "RoleChange",
            module: "User",
            entityName: nameof(ApplicationUser),
            recordId: user.Id,
            oldValue: currentRole,
            newValue: newRole,
            details: $"User {user.FullName} role changed from {currentRole} to {newRole}."
        );

        return true;
    }

    public async Task<bool> ToggleUserStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new DomainException("User not found.");

        // If currently active and is Admin, protect against deactivating last active admin
        if (user.IsActive && await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            var activeAdminCount = admins.Count(a => a.IsActive);
            if (activeAdminCount <= 1)
            {
                throw new DomainException("Cannot deactivate the only active Administrator.");
            }
        }

        var oldStatus = user.IsActive;
        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAsync(
            action: user.IsActive ? "Activate" : "Deactivate",
            module: "User",
            entityName: nameof(ApplicationUser),
            recordId: user.Id,
            oldValue: oldStatus.ToString(),
            newValue: user.IsActive.ToString(),
            details: $"User {user.FullName} status set to {(user.IsActive ? "Active" : "Inactive")}."
        );

        return true;
    }

    public async Task<bool> UnlockUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new DomainException("User not found.");

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditLog.LogAsync(
            action: "Unlock",
            module: "Security",
            entityName: nameof(ApplicationUser),
            recordId: user.Id,
            oldValue: "Locked",
            newValue: "Unlocked",
            details: $"Account lockout cleared for user {user.Email}."
        );

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new DomainException("User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new DomainException($"Password reset failed: {errors}");
        }

        await _auditLog.LogAsync(
            action: "PasswordReset",
            module: "Security",
            entityName: nameof(ApplicationUser),
            recordId: user.Id,
            oldValue: null,
            newValue: "PasswordResetByAdmin",
            details: $"Administrator triggered password reset for user {user.Email}."
        );

        return true;
    }

    public async Task<List<ApplicationUser>> GetSalesUsersAsync()
    {
        var salesExecs = await _userManager.GetUsersInRoleAsync(Roles.SalesExecutive);
        var managers = await _userManager.GetUsersInRoleAsync(Roles.Manager);
        var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);

        return salesExecs.Concat(managers).Concat(admins)
            .Where(u => u.IsActive)
            .DistinctBy(u => u.Id)
            .OrderBy(u => u.FullName)
            .ToList();
    }
}
