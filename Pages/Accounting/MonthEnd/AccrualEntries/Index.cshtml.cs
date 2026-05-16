using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.MonthEnd.AccrualEntries;

public class IndexModel(ApplicationDbContext context, AccountsPayableService accountsPayableService) : PageModel
{
    public IList<AccrualEntry> Accruals { get; private set; } = [];
    public SelectList PeriodOptions { get; private set; } = default!;
    public SelectList GLAccountOptions { get; private set; } = default!;

    [BindProperty]
    public AccrualEntry Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Input.AccrualNumber = await accountsPayableService.GenerateNumberAsync("ACCR");
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Description))
        {
            ModelState.AddModelError("Input.Description", "Description is required.");
        }

        if (Input.Amount <= 0)
        {
            ModelState.AddModelError("Input.Amount", "Amount must be positive.");
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

        Input.CreatedBy = User.Identity?.Name ?? "System";
        Input.Status = AccrualEntryStatus.Draft;
        context.AccrualEntries.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var errors = await accountsPayableService.ApproveAccrualAsync(id, User.Identity?.Name ?? "System");
        StatusMessage = errors.Count == 0 ? "Accrual approved." : string.Join(" ", errors);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPostAsync(int id)
    {
        var errors = await accountsPayableService.PostAccrualAsync(id, User.Identity?.Name ?? "System");
        StatusMessage = errors.Count == 0 ? "Accrual posted to GL." : string.Join(" ", errors);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReverseAsync(int id)
    {
        var errors = await accountsPayableService.ReverseAccrualAsync(id, User.Identity?.Name ?? "System");
        StatusMessage = errors.Count == 0 ? "Accrual reversed." : string.Join(" ", errors);
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Accruals = await context.AccrualEntries.AsNoTracking()
            .Include(accrual => accrual.AccountingPeriod)
            .Include(accrual => accrual.DebitGLAccount)
            .Include(accrual => accrual.CreditGLAccount)
            .OrderByDescending(accrual => accrual.AccrualDate)
            .ThenByDescending(accrual => accrual.Id)
            .Take(250)
            .ToListAsync();
        PeriodOptions = new SelectList(await context.AccountingPeriods.AsNoTracking().OrderByDescending(period => period.StartDate).ToListAsync(), "Id", "PeriodName");
        GLAccountOptions = new SelectList(await context.GLAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountCode).Select(account => new { account.Id, Name = account.AccountCode + " - " + account.AccountName }).ToListAsync(), "Id", "Name");
    }
}
