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
}
