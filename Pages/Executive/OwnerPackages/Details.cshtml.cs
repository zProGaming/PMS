using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Executive;

namespace Vantage.PMS.Pages.Executive.OwnerPackages;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    public OwnerReportPackage? Package { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Package = await context.OwnerReportPackages.AsNoTracking().Include(package => package.Items).FirstOrDefaultAsync(package => package.Id == id);
        return Package is null ? NotFound() : Page();
    }
}
