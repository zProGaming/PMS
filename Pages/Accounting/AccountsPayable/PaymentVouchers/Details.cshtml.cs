using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.AccountsPayable.PaymentVouchers;

public class DetailsModel(ApplicationDbContext context, AccountsPayableService accountsPayableService) : PageModel
{
    public PaymentVoucher Voucher { get; private set; } = default!;
    public SelectList BankAccountOptions { get; private set; } = default!;

    [BindProperty]
    public int? BankAccountId { get; set; }

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
        var voucher = await LoadVoucherAsync(id);
        if (voucher is null) return NotFound();
        Voucher = voucher;
        await LoadBankAccountsAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetReleaseNativeAsync(int id)
    {
        var voucher = await LoadVoucherAsync(id);
        if (voucher is null) return NotFound();
        Voucher = voucher;
        await LoadBankAccountsAsync();
        return NativeReleasePartial();
    }

    public Task<IActionResult> OnGetMarkForApprovalNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "MarkForApproval",
            "Mark voucher for approval",
            "Move this payment voucher into the AP approval queue.",
            "Mark For Approval",
            "vpms-btn-primary",
            "The voucher cannot be released until approval succeeds.");

    public Task<IActionResult> OnGetApproveNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Approve",
            "Approve payment voucher",
            "Approve this voucher for treasury release.",
            "Approve Voucher",
            "vpms-btn-primary",
            "Release and GL posting remain controlled by the separate payment release step.");

    public Task<IActionResult> OnGetCancelNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Cancel",
            "Cancel payment voucher",
            "Cancel this payment voucher? Draft and for-approval vouchers can be cancelled before release.",
            "Cancel Voucher",
            "vpms-btn-danger",
            "Cancelled vouchers remain visible for AP review.");

    public async Task<IActionResult> OnPostMarkForApprovalAsync(int id)
    {
        var voucher = await context.PaymentVouchers.FindAsync(id);
        if (voucher is not null && voucher.Status == PaymentVoucherStatus.Draft)
        {
            voucher.Status = PaymentVoucherStatus.ForApproval;
            await context.SaveChangesAsync();
            StatusMessage = "Payment voucher marked for approval.";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var errors = await accountsPayableService.ApprovePaymentVoucherAsync(id, User.Identity?.Name ?? "System");
        StatusMessage = errors.Count == 0 ? "Payment voucher approved." : string.Join(" ", errors);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReleaseAsync(int id)
    {
        var errors = await accountsPayableService.ReleasePaymentVoucherAsync(id, BankAccountId, User.Identity?.Name ?? "System");
        StatusMessage = errors.Count == 0 ? "Payment voucher released and posted to GL." : string.Join(" ", errors);
        if (errors.Count > 0 && IsNativeWorkflowRequest())
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var voucher = await LoadVoucherAsync(id);
            if (voucher is null) return NotFound();
            Voucher = voucher;
            await LoadBankAccountsAsync();
            return NativeReleasePartial();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var voucher = await context.PaymentVouchers.FindAsync(id);
        if (voucher is not null && voucher.Status is PaymentVoucherStatus.Draft or PaymentVoucherStatus.ForApproval)
        {
            voucher.Status = PaymentVoucherStatus.Cancelled;
            await context.SaveChangesAsync();
            StatusMessage = "Payment voucher cancelled.";
        }
        return RedirectToPage(new { id });
    }

    private async Task<PaymentVoucher?> LoadVoucherAsync(int id)
    {
        return await context.PaymentVouchers.AsNoTracking()
            .Include(voucher => voucher.Supplier)
            .Include(voucher => voucher.APInvoice)
            .Include(voucher => voucher.JournalEntry)
            .Include(voucher => voucher.Disbursements)
            .FirstOrDefaultAsync(voucher => voucher.Id == id);
    }

    private async Task LoadBankAccountsAsync()
    {
        BankAccountOptions = new SelectList(await context.BankAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountName).ToListAsync(), "Id", "AccountName");
    }

    private bool IsNativeWorkflowRequest()
    {
        return string.Equals(Request.Query["vpmsNative"], "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-VPMS-Native-Dialog"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private PartialViewResult NativeReleasePartial()
    {
        return new PartialViewResult
        {
            ViewName = "_ReleaseNative",
            ViewData = new ViewDataDictionary<DetailsModel>(ViewData, this)
        };
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
        var voucher = await LoadVoucherAsync(id);
        if (voucher is null)
        {
            return NotFound();
        }

        Voucher = voucher;
        await LoadBankAccountsAsync();
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
