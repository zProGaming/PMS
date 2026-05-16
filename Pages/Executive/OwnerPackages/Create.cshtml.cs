using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Authorization;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive.OwnerPackages;

public class CreateModel(OwnerReportPackageService packageService) : PageModel
{
    [BindProperty]
    public OwnerPackageInput Input { get; set; } = new();

    public void OnGet()
    {
        Input.PeriodStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        Input.PeriodEnd = DateTime.Today;
        Input.PackageName = $"Owner Report Package - {DateTime.Today:MMMM yyyy}";
        Input.PreparedFor = "Hotel Owner / General Manager";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!PmsRoles.ExecutiveManagement.Any(role => User.IsInRole(role)))
        {
            return Forbid();
        }

        if (Input.PeriodEnd < Input.PeriodStart)
        {
            ModelState.AddModelError(string.Empty, "Period end must be on or after period start.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var package = await packageService.CreateDefaultAsync(Input.PackageName, Input.PreparedFor, Input.PeriodStart, Input.PeriodEnd, User.Identity?.Name ?? "Executive");
        return RedirectToPage("/Executive/OwnerPackages/Details", new { id = package.Id });
    }
}

public class OwnerPackageInput
{
    public string PackageName { get; set; } = string.Empty;
    public string PreparedFor { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}
