using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        StockAdjustment.AdjustmentNumber = await _inventoryService.GenerateNumberAsync("ADJ");
        StockAdjustment.PreparedBy = User.Identity?.Name ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
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
}
