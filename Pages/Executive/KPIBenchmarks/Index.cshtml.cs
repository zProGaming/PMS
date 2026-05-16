using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Executive;

namespace Vantage.PMS.Pages.Executive.KPIBenchmarks;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<KPIBenchmarkSetting> Benchmarks { get; private set; } = [];
    public IList<ExecutiveKPI> Kpis { get; private set; } = [];

    [BindProperty]
    public KPIBenchmarkSetting Input { get; set; } = new();

    public async Task OnGetAsync(int? id)
    {
        await LoadAsync();
        if (id is not null)
        {
            Input = await context.KPIBenchmarkSettings.FirstOrDefaultAsync(setting => setting.Id == id.Value) ?? new KPIBenchmarkSetting();
        }
        else
        {
            Input.EffectiveFrom = DateTime.Today;
            Input.IsActive = true;
        }
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (Input.TargetValue < 0)
        {
            ModelState.AddModelError(nameof(Input.TargetValue), "Target value cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var existing = Input.Id == 0 ? null : await context.KPIBenchmarkSettings.FindAsync(Input.Id);
        if (existing is null)
        {
            context.KPIBenchmarkSettings.Add(Input);
        }
        else
        {
            existing.BenchmarkName = Input.BenchmarkName;
            existing.KPIName = Input.KPIName;
            existing.TargetValue = Input.TargetValue;
            existing.WarningThreshold = Input.WarningThreshold;
            existing.CriticalThreshold = Input.CriticalThreshold;
            existing.EffectiveFrom = Input.EffectiveFrom;
            existing.EffectiveTo = Input.EffectiveTo;
            existing.IsActive = Input.IsActive;
            existing.Notes = Input.Notes;
        }

        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        var setting = await context.KPIBenchmarkSettings.FindAsync(id);
        if (setting is not null)
        {
            setting.IsActive = false;
            await context.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Kpis = await context.ExecutiveKPIs.AsNoTracking().Where(kpi => kpi.IsActive).OrderBy(kpi => kpi.SortOrder).ToListAsync();
        Benchmarks = await context.KPIBenchmarkSettings.AsNoTracking().OrderBy(setting => setting.KPIName).ThenByDescending(setting => setting.EffectiveFrom).ToListAsync();
    }
}
