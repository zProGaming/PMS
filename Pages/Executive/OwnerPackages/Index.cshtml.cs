using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Executive;

namespace Vantage.PMS.Pages.Executive.OwnerPackages;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<OwnerReportPackage> Packages { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Packages = await context.OwnerReportPackages.AsNoTracking().OrderByDescending(package => package.PreparedAt).Take(50).ToListAsync();
    }
}
