using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.ContactPersons;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public ContactPerson ContactPerson { get; set; } = default!;

    public SelectList AccountOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var contact = await _context.ContactPersons.FindAsync(id);
        if (contact is null)
        {
            return NotFound();
        }

        ContactPerson = contact;
        await LoadAccountOptionsAsync(ContactPerson.SalesAccountId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAccountOptionsAsync(ContactPerson.SalesAccountId);
            return Page();
        }

        _context.Attach(ContactPerson).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index", new { accountId = ContactPerson.SalesAccountId });
    }

    private async Task LoadAccountOptionsAsync(object? selectedAccount = null)
    {
        var accounts = await _context.SalesAccounts
            .AsNoTracking()
            .OrderBy(account => account.AccountName)
            .Select(account => new { account.Id, account.AccountName })
            .ToListAsync();

        AccountOptions = new SelectList(accounts, "Id", "AccountName", selectedAccount);
    }
}
