using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Executive;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive.Alerts;

public class DetailsModel(ApplicationDbContext context, ExecutiveAlertService alertService) : PageModel
{
    public ExecutiveAlert? Alert { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Alert = await context.ExecutiveAlerts.AsNoTracking().FirstOrDefaultAsync(alert => alert.Id == id);
        return Alert is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        await alertService.ResolveAsync(id, User.Identity?.Name ?? "Executive");
        return RedirectToPage("/Executive/Alerts/Details", new { id });
    }
}
