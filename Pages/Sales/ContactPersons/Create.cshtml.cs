using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.ContactPersons;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public ContactPerson ContactPerson { get; set; } = new();

    public SelectList AccountOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? accountId)
    {
        if (accountId is not null)
        {
            ContactPerson.SalesAccountId = accountId.Value;
        }

        await LoadAccountOptionsAsync(accountId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAccountOptionsAsync(ContactPerson.SalesAccountId);
            return Page();
        }

        _context.ContactPersons.Add(ContactPerson);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index", new { accountId = ContactPerson.SalesAccountId });
    }

    private async Task LoadAccountOptionsAsync(object? selectedAccount = null)
    {
        var accounts = await _context.SalesAccounts
            .AsNoTracking()
            .Where(account => account.IsActive)
            .OrderBy(account => account.AccountName)
            .Select(account => new { account.Id, account.AccountName })
            .ToListAsync();

        AccountOptions = new SelectList(accounts, "Id", "AccountName", selectedAccount);
    }
}
