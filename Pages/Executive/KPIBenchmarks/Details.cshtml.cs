using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Executive;

namespace Vantage.PMS.Pages.Executive.KPIBenchmarks;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    public KPIBenchmarkSetting? Benchmark { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Benchmark = await context.KPIBenchmarkSettings.AsNoTracking().FirstOrDefaultAsync(setting => setting.Id == id);
        return Benchmark is null ? NotFound() : Page();
    }
}
