using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.PostingRules;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<PostingRule> Rules { get; private set; } = [];
    public SelectList AccountOptions { get; private set; } = default!;
    public SelectList DepartmentOptions { get; private set; } = default!;

    [BindProperty]
    public PostingRule Input { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.RuleName))
        {
            ModelState.AddModelError("Input.RuleName", "Rule name is required.");
        }

        if (Input.DebitGLAccountId == Input.CreditGLAccountId)
        {
            ModelState.AddModelError(string.Empty, "Debit and credit accounts must be different.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.IsActive = true;
        context.PostingRules.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        var rule = await context.PostingRules.FindAsync(id);
        if (rule is not null)
        {
            rule.IsActive = false;
            await context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Rules = await context.PostingRules
            .AsNoTracking()
            .Include(rule => rule.DebitGLAccount)
            .Include(rule => rule.CreditGLAccount)
            .OrderBy(rule => rule.SourceModule)
            .ThenBy(rule => rule.TransactionType)
            .ToListAsync();
        var accounts = await context.GLAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountCode).ToListAsync();
        AccountOptions = new SelectList(accounts.Select(account => new { account.Id, Name = $"{account.AccountCode} - {account.AccountName}" }), "Id", "Name");
        DepartmentOptions = new SelectList(await context.USALIDepartments.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.SortOrder).ToListAsync(), "Id", "Name");
    }
}
