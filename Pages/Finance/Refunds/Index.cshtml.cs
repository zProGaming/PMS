using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    public SelectList FolioOptions { get; set; } = null!;
    public SelectList PaymentOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Refund.RefundNumber = await _financeService.GenerateSimpleNumberAsync("REF");
        Refund.RequestedBy = User.Identity?.Name ?? string.Empty;
        await LoadAsync();
    }

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
}
