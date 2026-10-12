using AcxiomCRM.Core.Entities;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLog;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLog)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditLog = auditLog;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AuthLoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Invalid payload", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var cleanEmail = dto.Email.Trim().ToLower();
        var user = await _userManager.FindByEmailAsync(cleanEmail);

        if (user == null || !user.IsActive)
        {
            await _auditLog.LogAsync("ApiFailedLogin", "Auth", nameof(ApplicationUser), cleanEmail, result: "Failed");
            return Unauthorized(ApiResponse<string>.Fail("Invalid credentials or account inactive."));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            await _auditLog.LogAsync("ApiLogin", "Auth", nameof(ApplicationUser), user.Id, result: "Success");

            var response = new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? cleanEmail,
                Role = roles.FirstOrDefault() ?? "None"
            };

            return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Authenticated successfully."));
        }

        if (result.IsLockedOut)
        {
            await _auditLog.LogAsync("ApiLockout", "Security", nameof(ApplicationUser), user.Id, result: "Locked");
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<string>.Fail("Account is locked out due to multiple failed login attempts."));
        }

        await _auditLog.LogAsync("ApiFailedLogin", "Auth", nameof(ApplicationUser), user.Id, result: "Failed");
        return Unauthorized(ApiResponse<string>.Fail("Invalid credentials."));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(ApiResponse<string>.Ok("Logged out successfully."));
    }
}
