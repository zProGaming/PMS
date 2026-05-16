using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.TaxCodes;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<TaxCode> TaxCodes { get; private set; } = [];
    public SelectList AccountOptions { get; private set; } = default!;
    [BindProperty] public TaxCode Input { get; set; } = new();
    public async Task OnGetAsync() => await LoadAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.Rate < 0) ModelState.AddModelError("Input.Rate", "Tax rate cannot be negative.");
        if (await context.TaxCodes.AnyAsync(item => item.Code == Input.Code)) ModelState.AddModelError("Input.Code", "Code must be unique.");
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }
        Input.IsActive = true; context.TaxCodes.Add(Input); await context.SaveChangesAsync(); return RedirectToPage();
    }
    private async Task LoadAsync()
    {
        TaxCodes = await context.TaxCodes.AsNoTracking().Include(item => item.GLAccount).OrderBy(item => item.Code).ToListAsync();
        var accounts = await context.GLAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountCode).ToListAsync();
        AccountOptions = new SelectList(accounts.Select(account => new { account.Id, Name = $"{account.AccountCode} - {account.AccountName}" }), "Id", "Name");
    }
}
