using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Banking.BankReconciliations;

public class DetailsModel(ApplicationDbContext context, AccountsPayableService accountsPayableService) : PageModel
{
    public BankReconciliation Reconciliation { get; private set; } = default!;

    [BindProperty]
    public AdjustmentInput Adjustment { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var reconciliation = await LoadAsync(id);
        if (reconciliation is null) return NotFound();
        Reconciliation = reconciliation;
        return Page();
    }

    public async Task<IActionResult> OnPostToggleClearedAsync(int id, int itemId)
    {
        var item = await context.BankReconciliationItems.Include(row => row.BankReconciliation).FirstOrDefaultAsync(row => row.Id == itemId && row.BankReconciliationId == id);
        if (item is not null && item.BankReconciliation?.Status != BankReconciliationStatus.Approved)
        {
            item.IsCleared = !item.IsCleared;
            await context.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddAdjustmentAsync(int id)
    {
        var reconciliation = await context.BankReconciliations.FindAsync(id);
        if (reconciliation is not null && reconciliation.Status != BankReconciliationStatus.Approved)
        {
            context.BankReconciliationItems.Add(new BankReconciliationItem
            {
                BankReconciliationId = id,
                Description = Adjustment.Description,
                Amount = Adjustment.Amount,
                ItemType = Adjustment.ItemType,
                IsCleared = true,
                Notes = Adjustment.Notes
            });
            await context.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var errors = await accountsPayableService.ApproveBankReconciliationAsync(id, User.Identity?.Name ?? "System");
        StatusMessage = errors.Count == 0 ? "Bank reconciliation approved and cleared items locked." : string.Join(" ", errors);
        return RedirectToPage(new { id });
    }

    private async Task<BankReconciliation?> LoadAsync(int id)
    {
        var reconciliation = await context.BankReconciliations
            .Include(item => item.BankAccount)
            .Include(item => item.Items).ThenInclude(item => item.BankTransaction)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (reconciliation is not null)
        {
            await accountsPayableService.RecalculateBankReconciliationAsync(reconciliation);
            await context.SaveChangesAsync();
        }
        return reconciliation;
    }

    public class AdjustmentInput
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public BankReconciliationItemType ItemType { get; set; } = BankReconciliationItemType.Adjustment;
        public string? Notes { get; set; }
    }
}
