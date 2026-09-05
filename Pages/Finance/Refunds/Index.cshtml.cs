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

public class IndexModel(ApplicationDbContext context, FinanceAdjustmentService adjustments) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceAdjustmentService _adjustments = adjustments;

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
        Refund.RefundNumber = "Assigned On Save";
        Refund.RequestedBy = User.Identity?.Name ?? string.Empty;
        await LoadAsync();
    }

    public async Task<IActionResult> OnGetCreateNativeAsync()
    {
        Refund.RefundNumber = "Assigned On Save";
        Refund.RequestedBy = User.Identity?.Name ?? string.Empty;
        Refund.RefundDate = DateTime.Today;
        await LoadAsync();
        return NativeCreatePartial();
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
            "Record this approved refund against its source receipt and your open cashier shift. This records the payout; it does not send money through a payment gateway.",
            "Process Refund",
            "vpms-btn-primary",
            "Refunding a settled charge can reopen a balance due. Confirm the approved amount and payout evidence before processing.");

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
        if (ModelState.IsValid)
            foreach (var error in await _adjustments.CreateRefundAsync(Refund, User))
                ModelState.AddModelError(string.Empty, error);
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return IsNativeWorkflowRequest() ? NativeCreatePartial() : Page();
        }
        TempData["SuccessMessage"] = "Refund request created. A different manager must approve it.";
        return RedirectToPage();
    }

    public Task<IActionResult> OnPostApproveAsync(int id) => DecideAsync(id, "Approve");
    public Task<IActionResult> OnPostRejectAsync(int id) => DecideAsync(id, "Reject");
    public Task<IActionResult> OnPostProcessAsync(int id) => DecideAsync(id, "Process");
    public Task<IActionResult> OnPostCancelAsync(int id) => DecideAsync(id, "Cancel");

    private async Task<IActionResult> DecideAsync(int id, string action)
    {
        if (action is "Approve" or "Reject" && !CanApprove()) return Forbid();
        var errors = await _adjustments.DecideRefundAsync(id, action, User);
        TempData[errors.Count > 0 ? "ErrorMessage" : "SuccessMessage"] =
            errors.Count > 0 ? string.Join(" ", errors) : "Refund decision recorded. Review the current status below.";
        return RedirectToPage();
    }

    public bool CanReview(RefundTransaction refund) => CanApprove() &&
        !FinanceAdjustmentService.SameActor(refund.RequestedBy, User.Identity?.Name);
    public bool CanProcess(RefundTransaction refund) =>
        !FinanceAdjustmentService.SameActor(refund.ApprovedBy, User.Identity?.Name);
    public bool CanCancel(RefundTransaction refund) => CanApprove() ||
        FinanceAdjustmentService.SameActor(refund.RequestedBy, User.Identity?.Name);

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
        var payments = await _context.Payments.AsNoTracking().Where(payment => payment.Amount > 0 && payment.Status == PaymentStatus.Completed).OrderByDescending(payment => payment.PaymentDate).Select(payment => new { payment.Id, Name = "#" + payment.Id + " | " + payment.Folio!.FolioNumber + " | " + payment.Amount }).ToListAsync();
        FolioOptions = new SelectList(folios, "Id", "FolioNumber", Refund.FolioId);
        PaymentOptions = new SelectList(payments, "Id", "Name", Refund.PaymentId);
    }

    private bool CanApprove() =>
        User.IsInRole(PmsRoles.SystemAdmin) ||
        User.IsInRole(PmsRoles.GeneralManager) ||
        User.IsInRole(PmsRoles.FinanceManager);

    private bool IsNativeWorkflowRequest()
    {
        return string.Equals(Request.Query["vpmsNative"], "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-VPMS-Native-Dialog"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private PartialViewResult NativeCreatePartial()
    {
        return new PartialViewResult
        {
            ViewName = "_CreateNative",
            ViewData = new ViewDataDictionary<IndexModel>(ViewData, this)
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
