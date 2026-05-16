using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.AccountsPayable.APInvoices;

public class DetailsModel(ApplicationDbContext context, AccountsPayableService accountsPayableService) : PageModel
{
    public APInvoice Invoice { get; private set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice is null) return NotFound();
        Invoice = invoice;
        return Page();
    }

    public async Task<IActionResult> OnPostMarkForApprovalAsync(int id)
    {
        var invoice = await context.APInvoices.FindAsync(id);
        if (invoice is not null && invoice.Status == APInvoiceStatus.Draft)
        {
            invoice.Status = APInvoiceStatus.ForApproval;
            await context.SaveChangesAsync();
            StatusMessage = "AP invoice marked for approval.";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var errors = await accountsPayableService.ApproveAPInvoiceAsync(id, User.Identity?.Name ?? "System");
        StatusMessage = errors.Count == 0 ? "AP invoice approved and posted to GL." : string.Join(" ", errors);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var invoice = await context.APInvoices.FindAsync(id);
        if (invoice is not null && invoice.Status is APInvoiceStatus.Draft or APInvoiceStatus.ForApproval)
        {
            invoice.Status = APInvoiceStatus.Cancelled;
            await context.SaveChangesAsync();
            StatusMessage = "AP invoice cancelled.";
        }
        return RedirectToPage(new { id });
    }

    private async Task<APInvoice?> LoadInvoiceAsync(int id)
    {
        return await context.APInvoices.AsNoTracking()
            .Include(invoice => invoice.Supplier)
            .Include(invoice => invoice.PurchaseOrder)
            .Include(invoice => invoice.ReceivingRecord)
            .Include(invoice => invoice.JournalEntry)
            .Include(invoice => invoice.Lines).ThenInclude(line => line.GLAccount)
            .Include(invoice => invoice.Lines).ThenInclude(line => line.InventoryItem)
            .Include(invoice => invoice.PaymentVouchers)
            .FirstOrDefaultAsync(invoice => invoice.Id == id);
    }
}
