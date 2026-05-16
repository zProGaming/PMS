using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.ServiceChargeSettings;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<ServiceChargeSetting> Settings { get; private set; } = [];
    public SelectList AccountOptions { get; private set; } = default!;
    [BindProperty] public ServiceChargeSetting Input { get; set; } = new();
    public async Task OnGetAsync() => await LoadAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.Rate < 0) ModelState.AddModelError("Input.Rate", "Service charge rate cannot be negative.");
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }
        Input.IsActive = true; context.ServiceChargeSettings.Add(Input); await context.SaveChangesAsync(); return RedirectToPage();
    }
    private async Task LoadAsync()
    {
        Settings = await context.ServiceChargeSettings.AsNoTracking().Include(item => item.LiabilityGLAccount).Include(item => item.RevenueGLAccount).OrderBy(item => item.Name).ToListAsync();
        var accounts = await context.GLAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountCode).ToListAsync();
        AccountOptions = new SelectList(accounts.Select(account => new { account.Id, Name = $"{account.AccountCode} - {account.AccountName}" }), "Id", "Name");
    }
}
