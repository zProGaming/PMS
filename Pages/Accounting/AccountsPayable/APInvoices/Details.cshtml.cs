using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

    public string NativeActionHandler { get; private set; } = string.Empty;
    public string NativeActionTitle { get; private set; } = string.Empty;
    public string NativeActionMessage { get; private set; } = string.Empty;
    public string NativeActionButtonText { get; private set; } = string.Empty;
    public string NativeActionButtonClass { get; private set; } = "vpms-btn-primary";
    public string NativeActionSupport { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice is null) return NotFound();
        Invoice = invoice;
        return Page();
    }

    public Task<IActionResult> OnGetMarkForApprovalNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "MarkForApproval",
            "Mark AP invoice for approval",
            "Move this AP invoice into the approval queue for finance review.",
            "Mark For Approval",
            "vpms-btn-primary",
            "The invoice remains unposted until approval succeeds.");

    public Task<IActionResult> OnGetApproveNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Approve",
            "Approve and post AP invoice",
            "Approve this supplier invoice and post the configured AP journal entry.",
            "Approve & Post",
            "vpms-btn-primary",
            "Posting uses the existing AP service controls and will report setup errors if mappings are incomplete.");

    public Task<IActionResult> OnGetCancelNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Cancel",
            "Cancel AP invoice",
            "Cancel this AP invoice? Draft and for-approval invoices can be cancelled before payment processing.",
            "Cancel Invoice",
            "vpms-btn-danger",
            "Cancelled AP invoices remain visible for audit and supplier review.");

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

    private async Task<IActionResult> NativeConfirmAsync(
        int id,
        string handler,
        string title,
        string message,
        string buttonText,
        string buttonClass,
        string support)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice is null)
        {
            return NotFound();
        }

        Invoice = invoice;
        NativeActionHandler = handler;
        NativeActionTitle = title;
        NativeActionMessage = message;
        NativeActionButtonText = buttonText;
        NativeActionButtonClass = buttonClass;
        NativeActionSupport = support;

        return new PartialViewResult
        {
            ViewName = "_ConfirmActionNative",
            ViewData = new ViewDataDictionary<DetailsModel>(ViewData, this)
        };
    }
}
