using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Banking.BankAccounts;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<BankAccount> BankAccounts { get; private set; } = [];
    public SelectList GLAccountOptions { get; private set; } = default!;

    [BindProperty]
    public BankAccount Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.AccountName))
        {
            ModelState.AddModelError("Input.AccountName", "Account name is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.BankName))
        {
            ModelState.AddModelError("Input.BankName", "Bank name is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.IsActive = true;
        context.BankAccounts.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        var account = await context.BankAccounts.FindAsync(id);
        if (account is not null)
        {
            account.IsActive = false;
            await context.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        BankAccounts = await context.BankAccounts.AsNoTracking().Include(account => account.GLAccount).OrderBy(account => account.AccountName).ToListAsync();
        GLAccountOptions = new SelectList(await context.GLAccounts.AsNoTracking().Where(account => account.IsActive && account.AccountType == GLAccountType.Asset).OrderBy(account => account.AccountCode).Select(account => new { account.Id, Name = account.AccountCode + " - " + account.AccountName }).ToListAsync(), "Id", "Name");
    }
}
