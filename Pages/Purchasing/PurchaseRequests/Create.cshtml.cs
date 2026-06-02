using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Purchasing.PurchaseRequests;

public class CreateModel(ApplicationDbContext context, InventoryService inventoryService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly InventoryService _inventoryService = inventoryService;

    [BindProperty]
    public PurchaseRequest PurchaseRequest { get; set; } = new() { RequestDate = DateTime.Today };

    public SelectList DepartmentOptions { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        PurchaseRequest.RequestNumber = await _inventoryService.GenerateNumberAsync("PR");
        PurchaseRequest.RequestedBy = User.Identity?.Name ?? string.Empty;
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetNativeAsync()
    {
        PurchaseRequest.RequestNumber = await _inventoryService.GenerateNumberAsync("PR");
        PurchaseRequest.RequestedBy = User.Identity?.Name ?? string.Empty;
        await LoadOptionsAsync();
        return NativePartial();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return NativePartialOrPage();
        }

        if (string.IsNullOrWhiteSpace(PurchaseRequest.RequestNumber))
        {
            PurchaseRequest.RequestNumber = await _inventoryService.GenerateNumberAsync("PR");
        }

        PurchaseRequest.Status = PurchaseRequestStatus.Draft;
        _context.PurchaseRequests.Add(PurchaseRequest);
        await _context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = PurchaseRequest.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .OrderBy(department => department.Name)
            .ToListAsync();

        DepartmentOptions = new SelectList(departments, "Id", "Name", PurchaseRequest.DepartmentId);
    }

    private IActionResult NativePartialOrPage()
    {
        return IsNativeWorkflowRequest() ? NativePartial() : Page();
    }

    private bool IsNativeWorkflowRequest()
    {
        return string.Equals(Request.Query["vpmsNative"], "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-VPMS-Native-Dialog"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private PartialViewResult NativePartial()
    {
        return new PartialViewResult
        {
            ViewName = "_CreateNative",
            ViewData = new ViewDataDictionary<CreateModel>(ViewData, this)
        };
    }
}
