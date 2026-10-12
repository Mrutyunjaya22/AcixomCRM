using AcxiomCRM.Core.Entities;

namespace AcxiomCRM.Infrastructure.Services.Interfaces;

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync(string? searchTerm = null, string? role = null, bool? isActive = null);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<ApplicationUser> CreateUserAsync(string fullName, string email, string password, string role);
    Task<bool> UpdateUserRoleAsync(string userId, string newRole);
    Task<bool> ToggleUserStatusAsync(string userId);
    Task<bool> UnlockUserAsync(string userId);
    Task<bool> ResetPasswordAsync(string userId, string newPassword);
    Task<List<ApplicationUser>> GetSalesUsersAsync();
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
