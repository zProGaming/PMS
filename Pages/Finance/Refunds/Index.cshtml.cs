using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.Refunds;

public class IndexModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    public IList<RefundTransaction> Refunds { get; set; } = new List<RefundTransaction>();

    [BindProperty]
    public RefundTransaction Refund { get; set; } = new() { RefundDate = DateTime.Today };

    public RefundTransaction? NativeRefund { get; private set; }
    public string NativeActionHandler { get; private set; } = string.Empty;
    public string NativeActionTitle { get; private set; } = string.Empty;
    public string NativeActionMessage { get; private set; } = string.Empty;
    public string NativeActionButtonText { get; private set; } = string.Empty;
    public string NativeActionButtonClass { get; private set; } = "vpms-btn-primary";
    public string NativeActionSupport { get; private set; } = string.Empty;

    public SelectList FolioOptions { get; set; } = null!;
    public SelectList PaymentOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Refund.RefundNumber = await _financeService.GenerateSimpleNumberAsync("REF");
        Refund.RequestedBy = User.Identity?.Name ?? string.Empty;
        await LoadAsync();
    }

    public Task<IActionResult> OnGetApproveNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Approve",
            "Approve refund request",
            "Approve this refund request for cashier processing.",
            "Approve Refund",
            "vpms-btn-primary",
            "Processing remains a separate controlled step after approval.");

    public Task<IActionResult> OnGetRejectNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Reject",
            "Reject refund request",
            "Reject this refund request and keep the decision visible in the refund queue.",
            "Reject Refund",
            "vpms-btn-danger",
            "Use rejection when the request should not proceed to processing.");

    public Task<IActionResult> OnGetProcessNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Process",
            "Process refund",
            "Process this approved refund and record the negative payment/cashier transaction where applicable.",
            "Process Refund",
            "vpms-btn-primary",
            "The existing finance validation will prevent over-refunding a source payment.");

    public Task<IActionResult> OnGetCancelNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Cancel",
            "Cancel refund request",
            "Cancel this refund request before it is processed.",
            "Cancel Refund",
            "vpms-btn-danger",
            "Cancelled refunds remain in the queue history.");

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (Refund.Amount <= 0)
        {
            ModelState.AddModelError(nameof(Refund.Amount), "Refund amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Refund.RefundNumber))
        {
            Refund.RefundNumber = await _financeService.GenerateSimpleNumberAsync("REF");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Refund.Status = RefundStatus.Requested;
        _context.RefundTransactions.Add(Refund);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        if (!CanApprove())
        {
            return Forbid();
        }

        var refund = await _context.RefundTransactions.FindAsync(id);
        if (refund is null) return NotFound();
        if (refund.Status is RefundStatus.Requested or RefundStatus.ForApproval)
        {
            refund.Status = RefundStatus.Approved;
            refund.ApprovedBy = User.Identity?.Name ?? "System";
            refund.ApprovedAt = DateTime.Now;
        }
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        if (!CanApprove())
        {
            return Forbid();
        }

        var refund = await _context.RefundTransactions.FindAsync(id);
        if (refund is null) return NotFound();
        if (refund.Status is RefundStatus.Requested or RefundStatus.ForApproval)
        {
            refund.Status = RefundStatus.Rejected;
        }
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostProcessAsync(int id)
    {
        var refund = await _context.RefundTransactions.FindAsync(id);
        if (refund is null) return NotFound();
        if (refund.Status != RefundStatus.Approved)
        {
            TempData["ErrorMessage"] = "Only approved refunds can be processed.";
            return RedirectToPage();
        }

        Payment? sourcePayment = null;
        if (refund.PaymentId is not null)
        {
            sourcePayment = await _context.Payments.FindAsync(refund.PaymentId);
            if (sourcePayment is null)
            {
                TempData["ErrorMessage"] = "Source payment was not found.";
                return RedirectToPage();
            }

            var alreadyRefunded = await _context.RefundTransactions
                .Where(item => item.PaymentId == refund.PaymentId && item.Status == RefundStatus.Processed)
                .SumAsync(item => item.Amount);
            if (alreadyRefunded + refund.Amount > sourcePayment.Amount)
            {
                TempData["ErrorMessage"] = "Refund amount exceeds available refundable amount.";
                return RedirectToPage();
            }
        }

        var folioId = refund.FolioId ?? sourcePayment?.FolioId;
        if (folioId is not null)
        {
            var refundPayment = new Payment
            {
                FolioId = folioId.Value,
                Amount = -refund.Amount,
                PaymentMethod = $"Refund - {refund.RefundMethod}",
                PaymentDate = DateTime.Now,
                ReferenceNumber = refund.RefundNumber,
                Notes = refund.Reason,
                Status = PaymentStatus.Completed
            };
            _context.Payments.Add(refundPayment);
        }

        var shift = await _financeService.GetOpenShiftForUserAsync(User.Identity?.Name ?? "Cashier");
        if (shift is not null)
        {
            _context.CashierTransactions.Add(new CashierTransaction
            {
                CashierShiftId = shift.Id,
                FolioId = folioId,
                PaymentId = sourcePayment?.Id,
                TransactionDate = DateTime.Now,
                TransactionType = CashierTransactionType.Refund,
                Amount = refund.Amount,
                PaymentMethod = refund.RefundMethod,
                ReferenceNumber = refund.RefundNumber,
                Notes = refund.Reason,
                CreatedBy = User.Identity?.Name ?? "System"
            });
        }

        if (sourcePayment is not null && sourcePayment.Amount == refund.Amount)
        {
            sourcePayment.Status = PaymentStatus.Refunded;
        }

        refund.Status = RefundStatus.Processed;
        refund.ProcessedBy = User.Identity?.Name ?? "System";
        refund.ProcessedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var refund = await _context.RefundTransactions.FindAsync(id);
        if (refund is null) return NotFound();
        if (refund.Status != RefundStatus.Processed) refund.Status = RefundStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Refunds = await _context.RefundTransactions
            .AsNoTracking()
            .Include(refund => refund.Folio)
            .Include(refund => refund.Payment)
            .OrderByDescending(refund => refund.RefundDate)
            .Take(200)
            .ToListAsync();

        var folios = await _context.Folios.AsNoTracking().OrderByDescending(folio => folio.Id).Select(folio => new { folio.Id, folio.FolioNumber }).ToListAsync();
        var payments = await _context.Payments.AsNoTracking().Where(payment => payment.Amount > 0 && payment.Status == PaymentStatus.Completed).OrderByDescending(payment => payment.PaymentDate).Select(payment => new { payment.Id, Name = "#" + payment.Id + " - " + payment.Amount }).ToListAsync();
        FolioOptions = new SelectList(folios, "Id", "FolioNumber", Refund.FolioId);
        PaymentOptions = new SelectList(payments, "Id", "Name", Refund.PaymentId);
    }

    private bool CanApprove() =>
        User.IsInRole(PmsRoles.SystemAdmin) ||
        User.IsInRole(PmsRoles.GeneralManager) ||
        User.IsInRole(PmsRoles.FinanceManager);

    private async Task<IActionResult> NativeConfirmAsync(
        int id,
        string handler,
        string title,
        string message,
        string buttonText,
        string buttonClass,
        string support)
    {
        var refund = await _context.RefundTransactions.AsNoTracking()
            .Include(item => item.Folio)
            .Include(item => item.Payment)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (refund is null)
        {
            return NotFound();
        }

        NativeRefund = refund;
        NativeActionHandler = handler;
        NativeActionTitle = title;
        NativeActionMessage = message;
        NativeActionButtonText = buttonText;
        NativeActionButtonClass = buttonClass;
        NativeActionSupport = support;

        return new PartialViewResult
        {
            ViewName = "_ConfirmActionNative",
            ViewData = new ViewDataDictionary<IndexModel>(ViewData, this)
        };
    }
}
