using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.AccountsReceivable.ARAccounts;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<ARAccount> Accounts { get; set; } = new List<ARAccount>();

    [BindProperty]
    public ARAccount Account { get; set; } = new() { IsActive = true };

    public SelectList SalesAccountOptions { get; set; } = null!;

    public async Task OnGetAsync(int? id)
    {
        await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (Account.CreditLimit < 0)
        {
            ModelState.AddModelError("Account.CreditLimit", "Credit limit cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(Account.Id == 0 ? null : Account.Id);
            return Page();
        }

        if (Account.Id == 0)
        {
            _context.ARAccounts.Add(Account);
        }
        else
        {
            var existing = await _context.ARAccounts.FindAsync(Account.Id);
            if (existing is null)
            {
                return NotFound();
            }

            existing.SalesAccountId = Account.SalesAccountId;
            existing.AccountName = Account.AccountName;
            existing.AccountType = Account.AccountType;
            existing.ContactPerson = Account.ContactPerson;
            existing.Phone = Account.Phone;
            existing.Email = Account.Email;
            existing.BillingAddress = Account.BillingAddress;
            existing.CreditLimit = Account.CreditLimit;
            existing.IsActive = Account.IsActive;
            existing.Notes = Account.Notes;
        }

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadAsync(int? id)
    {
        Accounts = await _context.ARAccounts
            .AsNoTracking()
            .Include(account => account.SalesAccount)
            .OrderBy(account => account.AccountName)
            .ToListAsync();

        var salesAccounts = await _context.SalesAccounts
            .AsNoTracking()
            .OrderBy(account => account.AccountName)
            .ToListAsync();

        SalesAccountOptions = new SelectList(salesAccounts, "Id", "AccountName", Account.SalesAccountId);

        if (id is not null)
        {
            var existing = await _context.ARAccounts.AsNoTracking().FirstOrDefaultAsync(account => account.Id == id);
            if (existing is not null)
            {
                Account = existing;
                SalesAccountOptions = new SelectList(salesAccounts, "Id", "AccountName", Account.SalesAccountId);
            }
        }
    }
}
