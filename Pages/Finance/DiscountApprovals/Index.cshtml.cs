using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.Finance.DiscountApprovals;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<DiscountApproval> Discounts { get; set; } = new List<DiscountApproval>();

    [BindProperty]
    public DiscountApproval Discount { get; set; } = new() { RequestedAt = DateTime.Now };

    public DiscountApproval? NativeDiscount { get; private set; }
    public string NativeTarget { get; private set; } = string.Empty;
    public string NativeActionHandler { get; private set; } = string.Empty;
    public string NativeActionTitle { get; private set; } = string.Empty;
    public string NativeActionMessage { get; private set; } = string.Empty;
    public string NativeActionButtonText { get; private set; } = string.Empty;
    public string NativeActionButtonClass { get; private set; } = "vpms-btn-primary";
    public string NativeActionSupport { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        Discount.RequestedBy = User.Identity?.Name ?? string.Empty;
        Discounts = await _context.DiscountApprovals
            .AsNoTracking()
            .Include(discount => discount.Folio)
            .Include(discount => discount.FolioItem)
            .Include(discount => discount.POSOrder)
            .Include(discount => discount.BanquetEvent)
            .OrderByDescending(discount => discount.RequestedAt)
            .Take(200)
            .ToListAsync();
    }

    public Task<IActionResult> OnGetApproveNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Approve",
            "Approve discount",
            "Approve this discount request for application.",
            "Approve Discount",
            "vpms-btn-primary",
            "Application remains separate so finance can confirm the target before reducing balances.");

    public Task<IActionResult> OnGetRejectNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Reject",
            "Reject discount",
            "Reject this discount request and preserve the decision trail.",
            "Reject Discount",
            "vpms-btn-danger",
            "Use rejection when the requested discount should not affect the target transaction.");

    public Task<IActionResult> OnGetApplyNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Apply",
            "Apply approved discount",
            "Apply this approved discount to the selected folio, POS order, or banquet event.",
            "Apply Discount",
            "vpms-btn-primary",
            "The existing validation prevents a folio discount from creating a negative balance.");

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (Discount.DiscountAmount <= 0)
        {
            ModelState.AddModelError(nameof(Discount.DiscountAmount), "Discount amount must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        Discount.Status = Discount.DiscountType == Vantage.PMS.Models.Finance.DiscountType.Percentage && Discount.DiscountValue <= 10
            ? ApprovalStatus.Approved
            : ApprovalStatus.Pending;
        Discount.RequestedAt = DateTime.Now;
        if (Discount.Status == ApprovalStatus.Approved)
        {
            Discount.ApprovedBy = "Auto threshold";
            Discount.ApprovedAt = DateTime.Now;
        }
        _context.DiscountApprovals.Add(Discount);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        if (!CanApprove()) return Forbid();
        var discount = await _context.DiscountApprovals.FindAsync(id);
        if (discount is null) return NotFound();
        if (discount.Status == ApprovalStatus.Pending)
        {
            discount.Status = ApprovalStatus.Approved;
            discount.ApprovedBy = User.Identity?.Name ?? "System";
            discount.ApprovedAt = DateTime.Now;
        }
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        if (!CanApprove()) return Forbid();
        var discount = await _context.DiscountApprovals.FindAsync(id);
        if (discount is null) return NotFound();
        if (discount.Status == ApprovalStatus.Pending)
        {
            discount.Status = ApprovalStatus.Rejected;
        }
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApplyAsync(int id)
    {
        var discount = await _context.DiscountApprovals.FindAsync(id);
        if (discount is null) return NotFound();
        if (discount.Status != ApprovalStatus.Approved)
        {
            TempData["ErrorMessage"] = "Only approved discounts can be applied.";
            return RedirectToPage();
        }

        if (discount.FolioItemId is not null)
        {
            var item = await _context.FolioItems.FindAsync(discount.FolioItemId);
            if (item is not null)
            {
                item.Amount = Math.Max(0, item.Amount - discount.DiscountAmount);
            }
        }
        else if (discount.FolioId is not null)
        {
            var folio = await _context.Folios.Include(item => item.Items).Include(item => item.Payments).FirstOrDefaultAsync(item => item.Id == discount.FolioId);
            if (folio is not null && discount.DiscountAmount <= folio.Balance)
            {
                _context.FolioItems.Add(new FolioItem
                {
                    FolioId = folio.Id,
                    Description = "Approved discount",
                    ChargeCode = "DISC",
                    Quantity = 1,
                    UnitPrice = -discount.DiscountAmount,
                    Amount = -discount.DiscountAmount,
                    PostingDate = DateTime.Now,
                    PostedBy = User.Identity?.Name ?? "Finance"
                });
            }
            else
            {
                TempData["ErrorMessage"] = "Discount cannot make folio balance negative.";
                return RedirectToPage();
            }
        }
        else if (discount.POSOrderId is not null)
        {
            var order = await _context.POSOrders.FindAsync(discount.POSOrderId);
            if (order is not null)
            {
                order.DiscountAmount += discount.DiscountAmount;
                order.TotalAmount = Math.Max(0, order.TotalAmount - discount.DiscountAmount);
            }
        }
        else if (discount.BanquetEventId is not null)
        {
            _context.BanquetCharges.Add(new BanquetCharge
            {
                BanquetEventId = discount.BanquetEventId.Value,
                Description = "Approved discount",
                Quantity = 1,
                UnitPrice = -discount.DiscountAmount,
                Amount = -discount.DiscountAmount,
                ChargeDate = DateTime.Today
            });
        }

        discount.Status = ApprovalStatus.Cancelled;
        discount.Notes = string.IsNullOrWhiteSpace(discount.Notes) ? "Applied." : $"{discount.Notes} Applied.";
        await _context.SaveChangesAsync();
        return RedirectToPage();
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
        var discount = await _context.DiscountApprovals.AsNoTracking()
            .Include(item => item.Folio)
            .Include(item => item.FolioItem)
            .Include(item => item.POSOrder)
            .Include(item => item.BanquetEvent)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (discount is null)
        {
            return NotFound();
        }

        NativeDiscount = discount;
        NativeTarget = BuildTargetLabel(discount);
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

    private static string BuildTargetLabel(DiscountApproval discount) =>
        discount.FolioId is not null ? $"Folio #{discount.FolioId}" :
        discount.FolioItemId is not null ? $"Folio item #{discount.FolioItemId}" :
        discount.POSOrderId is not null ? $"POS order #{discount.POSOrderId}" :
        discount.BanquetEventId is not null ? $"Banquet event #{discount.BanquetEventId}" :
        "Unassigned";
}
