using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.VoidRequests;

public class IndexModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    public IList<VoidRequest> VoidRequests { get; set; } = new List<VoidRequest>();

    [BindProperty]
    public VoidRequest VoidRequest { get; set; } = new() { RequestedAt = DateTime.Now };

    public VoidRequest? NativeVoidRequest { get; private set; }
    public string NativeActionHandler { get; private set; } = string.Empty;
    public string NativeActionTitle { get; private set; } = string.Empty;
    public string NativeActionMessage { get; private set; } = string.Empty;
    public string NativeActionButtonText { get; private set; } = string.Empty;
    public string NativeActionButtonClass { get; private set; } = "vpms-btn-primary";
    public string NativeActionSupport { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        VoidRequest.RequestedBy = User.Identity?.Name ?? string.Empty;
        VoidRequests = await _context.VoidRequests
            .AsNoTracking()
            .OrderByDescending(request => request.RequestedAt)
            .Take(200)
            .ToListAsync();
    }

    public Task<IActionResult> OnGetApproveNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Approve",
            "Approve void request",
            "Approve this void request for processing.",
            "Approve Void",
            "vpms-btn-primary",
            "Processing remains separate so finance can review the source reference before voiding.");

    public Task<IActionResult> OnGetRejectNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Reject",
            "Reject void request",
            "Reject this void request and leave the decision in the control queue.",
            "Reject Void",
            "vpms-btn-danger",
            "Use rejection when the source transaction should remain active.");

    public Task<IActionResult> OnGetProcessNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Process",
            "Process approved void",
            "Process this approved void request through the existing finance service.",
            "Process Void",
            "vpms-btn-primary",
            "The service will validate the source reference before completing the void.");

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(VoidRequest.ReferenceType))
        {
            ModelState.AddModelError(nameof(VoidRequest.ReferenceType), "Reference type is required.");
        }

        if (VoidRequest.ReferenceId <= 0)
        {
            ModelState.AddModelError(nameof(VoidRequest.ReferenceId), "Reference ID is required.");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        VoidRequest.Status = ApprovalStatus.Pending;
        VoidRequest.RequestedAt = DateTime.Now;
        _context.VoidRequests.Add(VoidRequest);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        if (!CanApprove()) return Forbid();
        var request = await _context.VoidRequests.FindAsync(id);
        if (request is null) return NotFound();
        if (request.Status == ApprovalStatus.Pending)
        {
            request.Status = ApprovalStatus.Approved;
            request.ApprovedBy = User.Identity?.Name ?? "System";
            request.ApprovedAt = DateTime.Now;
        }
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        if (!CanApprove()) return Forbid();
        var request = await _context.VoidRequests.FindAsync(id);
        if (request is null) return NotFound();
        if (request.Status == ApprovalStatus.Pending)
        {
            request.Status = ApprovalStatus.Rejected;
            request.RejectedBy = User.Identity?.Name ?? "System";
            request.RejectedAt = DateTime.Now;
        }
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostProcessAsync(int id)
    {
        var errors = await _financeService.ProcessVoidRequestAsync(id, User.Identity?.Name ?? "System");
        if (errors.Count > 0)
        {
            TempData["ErrorMessage"] = string.Join(" ", errors);
        }
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
        var request = await _context.VoidRequests.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        NativeVoidRequest = request;
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
