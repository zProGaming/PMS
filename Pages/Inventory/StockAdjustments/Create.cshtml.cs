using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Inventory.StockAdjustments;

public class CreateModel(ApplicationDbContext context, InventoryService inventoryService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly InventoryService _inventoryService = inventoryService;

    [BindProperty]
    public StockAdjustment StockAdjustment { get; set; } = new() { AdjustmentDate = DateTime.Today };

    public async Task<IActionResult> OnGetAsync()
    {
        await PrepareInputAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetNativeAsync()
    {
        await PrepareInputAsync();
        return NativePartial();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return NativePartialOrPage();
        }

        if (string.IsNullOrWhiteSpace(StockAdjustment.AdjustmentNumber))
        {
            StockAdjustment.AdjustmentNumber = await _inventoryService.GenerateNumberAsync("ADJ");
        }

        StockAdjustment.Status = StockAdjustmentStatus.Draft;
        _context.StockAdjustments.Add(StockAdjustment);
        await _context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = StockAdjustment.Id });
    }

    private async Task PrepareInputAsync()
    {
        StockAdjustment.AdjustmentNumber = await _inventoryService.GenerateNumberAsync("ADJ");
        StockAdjustment.PreparedBy = User.Identity?.Name ?? string.Empty;
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
