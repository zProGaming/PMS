using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;

namespace Vantage.PMS.Pages.Admin.UsersAndRoles;

public class IndexModel(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext context) : PageModel
{
    public IList<UserAccessRow> Users { get; private set; } = new List<UserAccessRow>();

    public SelectList RoleOptions { get; private set; } = default!;

    [BindProperty]
    public CreateUserInput CreateUser { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var normalizedEmail = CreateUser.Email.Trim();
        var existing = await userManager.FindByEmailAsync(normalizedEmail);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(CreateUser.Email), "A user with this email address already exists.");
            await LoadAsync();
            return Page();
        }

        var roleName = await EnsureRoleAsync(CreateUser.RoleName);
        var user = new IdentityUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, CreateUser.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync();
            return Page();
        }

        var roleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync();
            return Page();
        }

        StatusMessage = $"User {normalizedEmail} created with {roleName} access. Bind the user to a company code before trial sign-in.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetRoleAsync(string id, string roleName)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            StatusMessage = "User not found.";
            return RedirectToPage();
        }

        var targetRole = await EnsureRoleAsync(roleName);
        var existingRoles = await userManager.GetRolesAsync(user);
        var removeResult = await userManager.RemoveFromRolesAsync(user, existingRoles);
        if (!removeResult.Succeeded)
        {
            StatusMessage = "Role update failed. Review Identity validation messages in the application logs.";
            return RedirectToPage();
        }

        var addResult = await userManager.AddToRoleAsync(user, targetRole);
        StatusMessage = addResult.Succeeded
            ? $"Role updated for {user.Email ?? user.UserName}."
            : "Role update failed. Review Identity validation messages in the application logs.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            StatusMessage = "User not found.";
            return RedirectToPage();
        }

        if (id == userManager.GetUserId(User))
        {
            StatusMessage = "You cannot deactivate your own administrator account.";
            return RedirectToPage();
        }

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        StatusMessage = $"User {user.Email ?? user.UserName} deactivated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReactivateAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            StatusMessage = "User not found.";
            return RedirectToPage();
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.SetLockoutEnabledAsync(user, true);
        StatusMessage = $"User {user.Email ?? user.UserName} reactivated.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        await EnsureConfiguredRolesAsync();

        var identityUsers = await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync();

        var accessCounts = await context.HotelUserAccesses
            .AsNoTracking()
            .Where(access => access.IsActive)
            .GroupBy(access => access.UserId)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.UserId, item => item.Count);

        var rows = new List<UserAccessRow>();
        foreach (var user in identityUsers)
        {
            var roles = await userManager.GetRolesAsync(user);
            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            rows.Add(new UserAccessRow
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? user.Id,
                UserName = user.UserName ?? string.Empty,
                Roles = roles.OrderBy(role => role).ToList(),
                IsLockedOut = isLocked,
                CompanyAccessCount = accessCounts.TryGetValue(user.Id, out var count) ? count : 0
            });
        }

        Users = rows;
        RoleOptions = new SelectList(PmsRoles.All.OrderBy(role => role));
    }

    private async Task EnsureConfiguredRolesAsync()
    {
        foreach (var role in PmsRoles.All)
        {
            await EnsureRoleAsync(role);
        }
    }

    private async Task<string> EnsureRoleAsync(string? roleName)
    {
        var safeRole = string.IsNullOrWhiteSpace(roleName) ? PmsRoles.FrontDesk : roleName.Trim();
        if (!PmsRoles.All.Contains(safeRole, StringComparer.OrdinalIgnoreCase))
        {
            safeRole = PmsRoles.FrontDesk;
        }

        if (!await roleManager.RoleExistsAsync(safeRole))
        {
            await roleManager.CreateAsync(new IdentityRole(safeRole));
        }

        return safeRole;
    }

    public class CreateUserInput
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(12, ErrorMessage = "Use at least 12 characters for trial and production accounts.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Initial Role")]
        public string RoleName { get; set; } = PmsRoles.FrontDesk;
    }

    public class UserAccessRow
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public IList<string> Roles { get; set; } = new List<string>();

        public bool IsLockedOut { get; set; }

        public int CompanyAccessCount { get; set; }
    }
}
