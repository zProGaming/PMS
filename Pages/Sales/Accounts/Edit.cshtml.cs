using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Accounts;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public SalesAccount SalesAccount { get; set; } = default!;

    public IEnumerable<SelectListItem> AccountTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var account = await _context.SalesAccounts.FindAsync(id);
        if (account is null)
        {
            return NotFound();
        }

        SalesAccount = account;
        LoadAccountTypeOptions();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateAccount();

        if (!ModelState.IsValid)
        {
            LoadAccountTypeOptions();
            return Page();
        }

        var account = await _context.SalesAccounts.FindAsync(SalesAccount.Id);
        if (account is null)
        {
            return NotFound();
        }

        account.AccountName = SalesAccount.AccountName;
        account.AccountType = SalesAccount.AccountType;
        account.Address = SalesAccount.Address;
        account.Phone = SalesAccount.Phone;
        account.Email = SalesAccount.Email;
        account.Website = SalesAccount.Website;
        account.CreditLimit = SalesAccount.CreditLimit;
        account.IsActive = SalesAccount.IsActive;
        account.Notes = SalesAccount.Notes;

        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private void ValidateAccount()
    {
        if (SalesAccount.CreditLimit < 0)
        {
            ModelState.AddModelError("SalesAccount.CreditLimit", "Credit limit cannot be negative.");
        }
    }

    private void LoadAccountTypeOptions()
    {
        AccountTypeOptions = Enum.GetValues<SalesAccountType>().Select(type => new SelectListItem
        {
            Value = type.ToString(),
            Text = type.ToString(),
            Selected = type == SalesAccount.AccountType
        });
    }
}
