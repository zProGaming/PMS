using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Inventory.StockIssue;

public class IndexModel(ApplicationDbContext context, InventoryService inventoryService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly InventoryService _inventoryService = inventoryService;

    [BindProperty]
    public int InventoryItemId { get; set; }

    [BindProperty]
    public int? DepartmentId { get; set; }

    [BindProperty]
    public decimal Quantity { get; set; }

    [BindProperty]
    public string? Remarks { get; set; }

    public SelectList InventoryItemOptions { get; set; } = null!;
    public SelectList DepartmentOptions { get; set; } = null!;

    public async Task OnGetAsync() => await LoadOptionsAsync();

    public async Task<IActionResult> OnGetNativeAsync()
    {
        await LoadOptionsAsync();
        return NativePartial();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Quantity <= 0)
        {
            ModelState.AddModelError(nameof(Quantity), "Quantity must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return NativePartialOrPage();
        }

        var errors = await _inventoryService.IssueStockAsync(InventoryItemId, DepartmentId, Quantity, Remarks, User.Identity?.Name ?? "System");
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await LoadOptionsAsync();
            return NativePartialOrPage();
        }

        TempData["SuccessMessage"] = "Stock issue posted.";
        return RedirectToPage();
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
            ViewName = "_IssueNative",
            ViewData = new ViewDataDictionary<IndexModel>(ViewData, this)
        };
    }

    private async Task LoadOptionsAsync()
    {
        var items = await _context.InventoryItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.ItemCode)
            .Select(item => new { item.Id, Name = item.ItemCode + " - " + item.ItemName + " (" + item.CurrentStock + " " + item.UnitOfMeasure + ")" })
            .ToListAsync();

        var departments = await _context.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .OrderBy(department => department.Name)
            .ToListAsync();

        InventoryItemOptions = new SelectList(items, "Id", "Name", InventoryItemId);
        DepartmentOptions = new SelectList(departments, "Id", "Name", DepartmentId);
    }
}
