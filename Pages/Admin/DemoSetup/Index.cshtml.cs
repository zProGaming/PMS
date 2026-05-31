using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Admin.DemoSetup;

public class IndexModel(DemoDataSeederService demoDataSeeder) : PageModel
{
    public DemoDataStatus Status { get; private set; } = new();
    public DemoSeedResult? Result { get; private set; }

    public async Task OnGetAsync()
    {
        Status = await demoDataSeeder.GetStatusAsync();
    }

    public Task<IActionResult> OnPostHotelAsync() => RunSeedAsync(demoDataSeeder.SeedDemoHotelAsync);

    public Task<IActionResult> OnPostOperationsAsync() => RunSeedAsync(demoDataSeeder.SeedDemoOperationsAsync);

    public Task<IActionResult> OnPostFinanceAsync() => RunSeedAsync(demoDataSeeder.SeedDemoFinanceAsync);

    public Task<IActionResult> OnPostFoodBeverageAsync() => RunSeedAsync(demoDataSeeder.SeedDemoFoodBeverageAsync);

    public Task<IActionResult> OnPostBanquetAsync() => RunSeedAsync(demoDataSeeder.SeedDemoBanquetAsync);

    public Task<IActionResult> OnPostInventoryAsync() => RunSeedAsync(demoDataSeeder.SeedDemoInventoryAsync);

    public Task<IActionResult> OnPostFinanceClosePackAsync() => RunSeedAsync(demoDataSeeder.SeedDemoFinanceClosePackAsync);

    public Task<IActionResult> OnPostFullAsync() => RunSeedAsync(demoDataSeeder.SeedFullDemoDatasetAsync);

    private async Task<IActionResult> RunSeedAsync(Func<string, Task<DemoSeedResult>> seedAction)
    {
        Result = await seedAction(User.Identity?.Name ?? "DemoSetup");
        Status = await demoDataSeeder.GetStatusAsync();
        return Page();
    }
}
