using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Inventory.StockAdjustments;

public class DetailsModel(ApplicationDbContext context, InventoryService inventoryService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly InventoryService _inventoryService = inventoryService;

    public StockAdjustment StockAdjustment { get; set; } = new();

    [BindProperty]
    public StockAdjustmentItem NewItem { get; set; } = new();

    public SelectList InventoryItemOptions { get; set; } = null!;

    public bool CanEditItems => StockAdjustment.Status == StockAdjustmentStatus.Draft;

    public string NativeActionHandler { get; private set; } = string.Empty;
    public string NativeActionTitle { get; private set; } = string.Empty;
    public string NativeActionMessage { get; private set; } = string.Empty;
    public string NativeActionButtonText { get; private set; } = string.Empty;
    public string NativeActionButtonClass { get; private set; } = "vpms-btn-primary";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var found = await LoadAsync(id);
        return found ? Page() : NotFound();
    }

    public Task<IActionResult> OnGetForApprovalNativeAsync(int id) =>
        NativeConfirmAsync(id, "ForApproval", "Submit stock adjustment", "Submit this stock adjustment for approval.", "Submit for Approval", "vpms-btn-primary");

    public Task<IActionResult> OnGetApproveNativeAsync(int id) =>
        NativeConfirmAsync(id, "Approve", "Approve stock adjustment", "Approve this stock adjustment for posting.", "Approve Adjustment", "vpms-btn-primary");

    public Task<IActionResult> OnGetPostAdjustmentNativeAsync(int id) =>
        NativeConfirmAsync(id, "PostAdjustment", "Post stock adjustment", "Post this approved adjustment and update stock balances.", "Post Adjustment", "vpms-btn-primary");

    public Task<IActionResult> OnGetCancelNativeAsync(int id) =>
        NativeConfirmAsync(id, "Cancel", "Cancel stock adjustment", "Cancel this stock adjustment and stop the inventory variance workflow.", "Cancel Adjustment", "vpms-btn-danger");

    public async Task<IActionResult> OnPostAddItemAsync(int id)
    {
        var found = await LoadAsync(id);
        if (!found)
        {
            return NotFound();
        }

        if (!CanEditItems)
        {
            ModelState.AddModelError(string.Empty, "Items can be edited only while the adjustment is draft.");
        }

        var inventoryItem = await _context.InventoryItems.AsNoTracking().FirstOrDefaultAsync(item => item.Id == NewItem.InventoryItemId);
        if (inventoryItem is null)
        {
            ModelState.AddModelError(nameof(NewItem.InventoryItemId), "Inventory item was not found.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        NewItem.StockAdjustmentId = id;
        NewItem.SystemQuantity = inventoryItem!.CurrentStock;
        NewItem.UnitCost = inventoryItem.StandardCost;
        NewItem.VarianceQuantity = NewItem.ActualQuantity - NewItem.SystemQuantity;
        NewItem.VarianceAmount = NewItem.VarianceQuantity * NewItem.UnitCost;
        _context.StockAdjustmentItems.Add(NewItem);
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteItemAsync(int id, int itemId)
    {
        var adjustment = await _context.StockAdjustments.FindAsync(id);
        if (adjustment is null)
        {
            return NotFound();
        }

        if (adjustment.Status != StockAdjustmentStatus.Draft)
        {
            TempData["ErrorMessage"] = "Items can be removed only while the adjustment is draft.";
            return RedirectToPage(new { id });
        }

        var item = await _context.StockAdjustmentItems.FirstOrDefaultAsync(line => line.Id == itemId && line.StockAdjustmentId == id);
        if (item is not null)
        {
            _context.StockAdjustmentItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostForApprovalAsync(int id)
    {
        var adjustment = await _context.StockAdjustments.Include(stockAdjustment => stockAdjustment.Items).FirstOrDefaultAsync(stockAdjustment => stockAdjustment.Id == id);
        if (adjustment is null)
        {
            return NotFound();
        }

        if (adjustment.Status != StockAdjustmentStatus.Draft)
        {
            TempData["ErrorMessage"] = "Only draft adjustments can be submitted for approval.";
            return RedirectToPage(new { id });
        }

        if (adjustment.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "Add at least one item before submitting for approval.";
            return RedirectToPage(new { id });
        }

        adjustment.Status = StockAdjustmentStatus.ForApproval;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var adjustment = await _context.StockAdjustments.FindAsync(id);
        if (adjustment is null)
        {
            return NotFound();
        }

        if (adjustment.Status != StockAdjustmentStatus.ForApproval)
        {
            TempData["ErrorMessage"] = "Only adjustments for approval can be approved.";
            return RedirectToPage(new { id });
        }

        adjustment.Status = StockAdjustmentStatus.Approved;
        adjustment.ApprovedBy = User.Identity?.Name ?? "System";
        adjustment.ApprovedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPostAdjustmentAsync(int id)
    {
        var errors = await _inventoryService.PostStockAdjustmentAsync(id, User.Identity?.Name ?? "System");
        if (errors.Count > 0)
        {
            TempData["ErrorMessage"] = string.Join(" ", errors);
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var adjustment = await _context.StockAdjustments.FindAsync(id);
        if (adjustment is null)
        {
            return NotFound();
        }

        if (adjustment.Status == StockAdjustmentStatus.Posted)
        {
            TempData["ErrorMessage"] = "Posted adjustments cannot be cancelled.";
            return RedirectToPage(new { id });
        }

        adjustment.Status = StockAdjustmentStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var adjustment = await _context.StockAdjustments
            .AsNoTracking()
            .Include(stockAdjustment => stockAdjustment.Items)
                .ThenInclude(item => item.InventoryItem)
            .FirstOrDefaultAsync(stockAdjustment => stockAdjustment.Id == id);

        if (adjustment is null)
        {
            return false;
        }

        StockAdjustment = adjustment;
        await LoadOptionsAsync();
        return true;
    }

    private async Task LoadOptionsAsync()
    {
        var items = await _context.InventoryItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.ItemCode)
            .Select(item => new { item.Id, Name = item.ItemCode + " - " + item.ItemName + " (" + item.CurrentStock + " " + item.UnitOfMeasure + ")" })
            .ToListAsync();

        InventoryItemOptions = new SelectList(items, "Id", "Name");
    }

    private async Task<IActionResult> NativeConfirmAsync(
        int id,
        string handler,
        string title,
        string message,
        string buttonText,
        string buttonClass)
    {
        var found = await LoadAsync(id);
        if (!found)
        {
            return NotFound();
        }

        NativeActionHandler = handler;
        NativeActionTitle = title;
        NativeActionMessage = message;
        NativeActionButtonText = buttonText;
        NativeActionButtonClass = buttonClass;

        return new PartialViewResult
        {
            ViewName = "_ConfirmActionNative",
            ViewData = new ViewDataDictionary<DetailsModel>(ViewData, this)
        };
    }
}
