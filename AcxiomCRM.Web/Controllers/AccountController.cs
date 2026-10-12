using AcxiomCRM.Core.Constants;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLog,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditLog = auditLog;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cleanEmail = model.Email.Trim().ToLower();
        var user = await _userManager.FindByEmailAsync(cleanEmail);

        if (user == null)
        {
            await _auditLog.LogAsync(
                action: "FailedLogin",
                module: "Auth",
                entityName: nameof(ApplicationUser),
                recordId: cleanEmail,
                result: "Failed",
                details: "Login attempt with non-existent email."
            );

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        if (!user.IsActive)
        {
            await _auditLog.LogAsync(
                action: "FailedLogin",
                module: "Auth",
                entityName: nameof(ApplicationUser),
                recordId: user.Id,
                overrideUserId: user.Id,
                overrideUserName: user.UserName,
                result: "Rejected",
                details: "Login attempt on deactivated account."
            );

            ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact an administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            userName: cleanEmail,
            password: model.Password,
            isPersistent: model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _auditLog.LogAsync(
                action: "Login",
                module: "Auth",
                entityName: nameof(ApplicationUser),
                recordId: user.Id,
                overrideUserId: user.Id,
                overrideUserName: user.UserName,
                result: "Success",
                details: "User successfully authenticated."
            );

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _auditLog.LogAsync(
                action: "Lockout",
                module: "Security",
                entityName: nameof(ApplicationUser),
                recordId: user.Id,
                overrideUserId: user.Id,
                overrideUserName: user.UserName,
                result: "Locked",
                details: "Account locked out due to multiple consecutive failed login attempts."
            );

            ModelState.AddModelError(string.Empty, "Account is locked out due to multiple failed login attempts. Please try again later or contact an administrator.");
            return View(model);
        }

        await _auditLog.LogAsync(
            action: "FailedLogin",
            module: "Auth",
            entityName: nameof(ApplicationUser),
            recordId: user.Id,
            overrideUserId: user.Id,
            overrideUserName: user.UserName,
            result: "Failed",
            details: $"Incorrect password. Failed attempt count: {user.AccessFailedCount + 1}."
        );

        ModelState.AddModelError(string.Empty, "Invalid login credentials.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cleanEmail = model.Email.Trim().ToLower();
        var existing = await _userManager.FindByEmailAsync(cleanEmail);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
            return View(model);
        }

        // Validate assigned role
        var selectedRole = model.Role;
        if (!Roles.All.Contains(selectedRole))
        {
            selectedRole = Roles.SalesExecutive;
        }

        var user = new ApplicationUser
        {
            UserName = cleanEmail,
            Email = cleanEmail,
            FullName = model.FullName.Trim(),
            IsActive = true,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, selectedRole);

            await _auditLog.LogAsync(
                action: "Register",
                module: "Auth",
                entityName: nameof(ApplicationUser),
                recordId: user.Id,
                overrideUserId: user.Id,
                overrideUserName: user.UserName,
                result: "Success",
                details: $"User registered with role {selectedRole}."
            );

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name;
        var userId = _userManager.GetUserId(User);

        await _signInManager.SignOutAsync();

        if (!string.IsNullOrEmpty(userId))
        {
            await _auditLog.LogAsync(
                action: "Logout",
                module: "Auth",
                entityName: nameof(ApplicationUser),
                recordId: userId,
                overrideUserId: userId,
                overrideUserName: userName,
                result: "Success",
                details: "User logged out."
            );
        }

        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
