using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;

namespace Vantage.PMS.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    ApplicationDbContext context,
    ILogger<LoginModel> logger) : PageModel
{
    private const string CompanyCodeClaimType = "Vantage.CompanyCode";
    private const string CompanyNameClaimType = "Vantage.CompanyName";
    private const string HotelIdClaimType = "Vantage.HotelId";
    private const string GlobalAdminClaimType = "Vantage.IsGlobalAdmin";

    private static readonly string[] GlobalAdminCompanyCodes = ["VANTAGE", "GLOBAL", "ADMIN"];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(40)]
        [Display(Name = "Company Code")]
        public string CompanyCode { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var companyCode = NormalizeCompanyCode(Input.CompanyCode);
        Input.CompanyCode = companyCode;

        var hotel = await context.Hotels
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Code.ToUpper() == companyCode);

        var user = await userManager.FindByEmailAsync(Input.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid company code, email, or password.");
            return Page();
        }

        var result = await signInManager.CheckPasswordSignInAsync(
            user,
            Input.Password,
            lockoutOnFailure: false);

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User account locked out.");
            return RedirectToPage("./Lockout");
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid company code, email, or password.");
            return Page();
        }

        var isSystemAdmin = await userManager.IsInRoleAsync(user, PmsRoles.SystemAdmin);
        var isGlobalAdminCode = GlobalAdminCompanyCodes.Contains(companyCode, StringComparer.OrdinalIgnoreCase);

        if (hotel is null && !(isSystemAdmin && isGlobalAdminCode))
        {
            ModelState.AddModelError(nameof(Input.CompanyCode), "Company Code was not found.");
            return Page();
        }

        if (!isSystemAdmin && hotel is not null)
        {
            var hasCompanyAccess = await context.HotelUserAccesses
                .AsNoTracking()
                .AnyAsync(access =>
                    access.UserId == user.Id &&
                    access.HotelId == hotel.Id &&
                    access.IsActive);

            if (!hasCompanyAccess)
            {
                ModelState.AddModelError(string.Empty, "Your account is not assigned to this company.");
                return Page();
            }
        }

        var companyName = hotel?.Name ?? "Vantage Global Admin";
        var claims = new List<Claim>
        {
            new(CompanyCodeClaimType, companyCode),
            new(CompanyNameClaimType, companyName),
            new(GlobalAdminClaimType, (isSystemAdmin && (hotel is null || isGlobalAdminCode)).ToString())
        };

        if (hotel is not null)
        {
            claims.Add(new Claim(HotelIdClaimType, hotel.Id.ToString()));
        }

        await signInManager.SignInWithClaimsAsync(user, Input.RememberMe, claims);
        logger.LogInformation("User logged in to company {CompanyCode}.", companyCode);
        return LocalRedirect(returnUrl);
    }

    private static string NormalizeCompanyCode(string value) =>
        value.Trim().ToUpperInvariant();
}
