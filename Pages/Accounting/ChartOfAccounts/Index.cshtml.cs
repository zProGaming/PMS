using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.ChartOfAccounts;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<GLAccount> Accounts { get; private set; } = [];

    public SelectList GLAccountOptions { get; private set; } = default!;

    public SelectList DepartmentOptions { get; private set; } = default!;

    public SelectList ReportLineOptions { get; private set; } = default!;

    [BindProperty]
    public GLAccount Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.AccountCode))
        {
            ModelState.AddModelError("Input.AccountCode", "Account code is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.AccountName))
        {
            ModelState.AddModelError("Input.AccountName", "Account name is required.");
        }

        if (await context.GLAccounts.AnyAsync(account => account.AccountCode == Input.AccountCode))
        {
            ModelState.AddModelError("Input.AccountCode", "Account code must be unique.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.IsActive = true;
        Input.CreatedAt = DateTime.Now;
        Input.CreatedBy = User.Identity?.Name ?? "System";
        context.GLAccounts.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        var account = await context.GLAccounts.FindAsync(id);
        if (account is null)
        {
            return RedirectToPage();
        }

        var used = await context.JournalEntryLines.AnyAsync(line => line.GLAccountId == id);
        if (!used)
        {
            account.IsActive = false;
            await context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Accounts = await context.GLAccounts
            .AsNoTracking()
            .Include(account => account.UsaliDepartment)
            .Include(account => account.UsaliReportLine)
            .OrderBy(account => account.AccountCode)
            .ToListAsync();

        GLAccountOptions = new SelectList(Accounts.Where(account => account.IsActive), "Id", "AccountName");
        DepartmentOptions = new SelectList(await context.USALIDepartments.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.SortOrder).ToListAsync(), "Id", "Name");
        ReportLineOptions = new SelectList(await context.USALIReportLines.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.SortOrder).ToListAsync(), "Id", "Name");
    }
}
