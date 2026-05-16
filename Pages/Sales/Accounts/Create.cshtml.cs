using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Accounts;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public SalesAccount SalesAccount { get; set; } = new() { IsActive = true };

    public IEnumerable<SelectListItem> AccountTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public void OnGet()
    {
        LoadAccountTypeOptions();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateAccount();

        if (!ModelState.IsValid)
        {
            LoadAccountTypeOptions();
            return Page();
        }

        SalesAccount.CreatedAt = DateTime.Now;
        SalesAccount.CreatedBy = User.Identity?.Name ?? Environment.UserName;

        _context.SalesAccounts.Add(SalesAccount);
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
