using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Labor;

namespace Vantage.PMS.Pages.Labor.PayrollCostEntries;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<PayrollCostEntry> Entries { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Entries = await context.PayrollCostEntries
            .AsNoTracking()
            .Include(entry => entry.PayrollPeriod)
            .Include(entry => entry.EmployeeCostProfile)
            .Include(entry => entry.Department)
            .Include(entry => entry.USALIDepartment)
            .OrderByDescending(entry => entry.PayrollPeriod != null ? entry.PayrollPeriod.StartDate : DateTime.MinValue)
            .ThenBy(entry => entry.EmployeeCostProfile != null ? entry.EmployeeCostProfile.FullName : entry.Position)
            .Take(300)
            .ToListAsync();
    }
}
