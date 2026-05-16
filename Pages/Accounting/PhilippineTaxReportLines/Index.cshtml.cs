using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.PhilippineTaxReportLines;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<PhilippineTaxReportLine> Lines { get; private set; } = [];
    public SelectList AccountOptions { get; private set; } = default!;
    [BindProperty] public PhilippineTaxReportLine Input { get; set; } = new();
    public async Task OnGetAsync() => await LoadAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }
        Input.IsActive = true; context.PhilippineTaxReportLines.Add(Input); await context.SaveChangesAsync(); return RedirectToPage();
    }
    private async Task LoadAsync()
    {
        Lines = await context.PhilippineTaxReportLines.AsNoTracking().Include(item => item.GLAccount).OrderBy(item => item.ReportType).ThenBy(item => item.SortOrder).ToListAsync();
        var accounts = await context.GLAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountCode).ToListAsync();
        AccountOptions = new SelectList(accounts.Select(account => new { account.Id, Name = $"{account.AccountCode} - {account.AccountName}" }), "Id", "Name");
    }
}
