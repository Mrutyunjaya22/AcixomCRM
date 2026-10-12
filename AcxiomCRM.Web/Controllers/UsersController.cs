using AcxiomCRM.Core.Constants;
using AcxiomCRM.Core.Exceptions;
using AcxiomCRM.Infrastructure.Services.Interfaces;
using AcxiomCRM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class UsersController : Controller
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search = null, string? role = null, bool? isActive = null)
    {
        var users = await _userService.GetUsersAsync(search, role, isActive);
        var model = new UserListViewModel
        {
            Users = users,
            SearchTerm = search,
            Role = role,
            IsActive = isActive,
            RolesList = new SelectList(Roles.All, role)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new UserCreateViewModel
        {
            RolesList = new SelectList(Roles.All, Roles.SalesExecutive)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.RolesList = new SelectList(Roles.All, model.Role);
            return View(model);
        }

        try
        {
            var user = await _userService.CreateUserAsync(model.FullName, model.Email, model.Password, model.Role);
            TempData["SuccessMessage"] = $"User '{user.FullName}' ({user.Email}) created with role {model.Role}.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.RolesList = new SelectList(Roles.All, model.Role);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditRole(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();

        var model = new UserEditRoleViewModel
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            RolesList = new SelectList(Roles.All, user.Role)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRole(string id, UserEditRoleViewModel model)
    {
        if (id != model.UserId) return BadRequest();

        if (!ModelState.IsValid)
        {
            model.RolesList = new SelectList(Roles.All, model.Role);
            return View(model);
        }

        try
        {
            await _userService.UpdateUserRoleAsync(id, model.Role);
            TempData["SuccessMessage"] = $"Role for {model.FullName} updated to {model.Role}.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.RolesList = new SelectList(Roles.All, model.Role);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        try
        {
            await _userService.ToggleUserStatusAsync(id);
            TempData["SuccessMessage"] = "User status updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        try
        {
            await _userService.UnlockUserAsync(id);
            TempData["SuccessMessage"] = "Account lockout has been cleared.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();

        var model = new UserResetPasswordViewModel
        {
            UserId = user.Id,
            Email = user.Email
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, UserResetPasswordViewModel model)
    {
        if (id != model.UserId) return BadRequest();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _userService.ResetPasswordAsync(id, model.NewPassword);
            TempData["SuccessMessage"] = $"Password for {model.Email} has been reset.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
