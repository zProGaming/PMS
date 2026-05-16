using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Banking.BankReconciliations;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<BankReconciliation> Reconciliations { get; private set; } = [];
    public SelectList BankAccountOptions { get; private set; } = default!;

    [BindProperty]
    public BankReconciliation Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var bankAccount = await context.BankAccounts.AsNoTracking().FirstOrDefaultAsync(account => account.Id == Input.BankAccountId && account.IsActive);
        if (bankAccount is null)
        {
            ModelState.AddModelError("Input.BankAccountId", "Active bank account is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.PreparedBy = User.Identity?.Name ?? "System";
        Input.Status = BankReconciliationStatus.Draft;
        Input.BookEndingBalance = bankAccount!.OpeningBalance;
        Input.Items = await context.BankTransactions.AsNoTracking()
            .Where(transaction => transaction.BankAccountId == Input.BankAccountId && !transaction.IsReconciled && transaction.TransactionDate <= Input.ReconciliationDate)
            .OrderBy(transaction => transaction.TransactionDate)
            .Select(transaction => new BankReconciliationItem
            {
                BankTransactionId = transaction.Id,
                Description = transaction.Description,
                Amount = transaction.DebitAmount - transaction.CreditAmount,
                ItemType = transaction.CreditAmount > 0 ? BankReconciliationItemType.Withdrawal : BankReconciliationItemType.Deposit,
                IsCleared = false
            })
            .ToListAsync();

        context.BankReconciliations.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = Input.Id });
    }

    private async Task LoadAsync()
    {
        Reconciliations = await context.BankReconciliations.AsNoTracking().Include(item => item.BankAccount).OrderByDescending(item => item.ReconciliationDate).ThenByDescending(item => item.Id).Take(100).ToListAsync();
        BankAccountOptions = new SelectList(await context.BankAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountName).ToListAsync(), "Id", "AccountName");
    }
}
