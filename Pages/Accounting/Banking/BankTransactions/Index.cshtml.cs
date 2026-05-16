using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Banking.BankTransactions;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<BankTransaction> Transactions { get; private set; } = [];
    public SelectList BankAccountOptions { get; private set; } = default!;

    [BindProperty]
    public BankTransaction Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (Input.BankAccountId <= 0)
        {
            ModelState.AddModelError("Input.BankAccountId", "Bank account is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.Description))
        {
            ModelState.AddModelError("Input.Description", "Description is required.");
        }

        if ((Input.DebitAmount <= 0 && Input.CreditAmount <= 0) || (Input.DebitAmount > 0 && Input.CreditAmount > 0))
        {
            ModelState.AddModelError(string.Empty, "Enter either a debit or credit amount.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.SourceModule = SourceModule.Manual;
        context.BankTransactions.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Transactions = await context.BankTransactions.AsNoTracking().Include(transaction => transaction.BankAccount).OrderByDescending(transaction => transaction.TransactionDate).ThenByDescending(transaction => transaction.Id).Take(250).ToListAsync();
        BankAccountOptions = new SelectList(await context.BankAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountName).ToListAsync(), "Id", "AccountName");
    }
}
