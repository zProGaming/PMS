using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.VoidRequests;

public class IndexModel(ApplicationDbContext context, FinanceAdjustmentService adjustments) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceAdjustmentService _adjustments = adjustments;

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

    public IActionResult OnGetCreateNative()
    {
        VoidRequest.RequestedBy = User.Identity?.Name ?? string.Empty;
        VoidRequest.RequestedAt = DateTime.Now;
        return NativeCreatePartial();
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
        if (ModelState.IsValid)
            foreach (var error in await _adjustments.CreateVoidAsync(VoidRequest, User))
                ModelState.AddModelError(string.Empty, error);
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return IsNativeWorkflowRequest() ? NativeCreatePartial() : Page();
        }
        TempData["SuccessMessage"] = "Void request created. A different manager must approve it.";
        return RedirectToPage();
    }

    public Task<IActionResult> OnPostApproveAsync(int id) => DecideAsync(id, "Approve");
    public Task<IActionResult> OnPostRejectAsync(int id) => DecideAsync(id, "Reject");
    public Task<IActionResult> OnPostProcessAsync(int id) => DecideAsync(id, "Process");

    private async Task<IActionResult> DecideAsync(int id, string action)
    {
        if (action is "Approve" or "Reject" && !CanApprove()) return Forbid();
        var errors = await _adjustments.DecideVoidAsync(id, action, User);
        TempData[errors.Count > 0 ? "ErrorMessage" : "SuccessMessage"] =
            errors.Count > 0 ? string.Join(" ", errors) : "Void decision recorded. Review the current status below.";
        return RedirectToPage();
    }

    public bool CanReview(VoidRequest request) => CanApprove() &&
        !FinanceAdjustmentService.SameActor(request.RequestedBy, User.Identity?.Name);
    public bool CanProcess(VoidRequest request) =>
        !FinanceAdjustmentService.SameActor(request.ApprovedBy, User.Identity?.Name);

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
