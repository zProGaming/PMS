using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Purchasing.Receiving;

public class DetailsModel(ApplicationDbContext context, InventoryService inventoryService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly InventoryService _inventoryService = inventoryService;

    public ReceivingRecord ReceivingRecord { get; set; } = new();

    [BindProperty]
    public ReceivingRecordItem NewItem { get; set; } = new();

    public SelectList InventoryItemOptions { get; set; } = null!;

    public bool CanEditItems => ReceivingRecord.Status == ReceivingStatus.Draft;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var found = await LoadAsync(id);
        return found ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostAddItemAsync(int id)
    {
        var found = await LoadAsync(id);
        if (!found)
        {
            return NotFound();
        }

        if (!CanEditItems)
        {
            ModelState.AddModelError(string.Empty, "Posted receiving records cannot be edited.");
        }

        if (NewItem.QuantityReceived <= 0)
        {
            ModelState.AddModelError(nameof(NewItem.QuantityReceived), "Quantity received must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        NewItem.ReceivingRecordId = id;
        _inventoryService.RecalculateReceivingItem(NewItem);
        _context.ReceivingRecordItems.Add(NewItem);
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteItemAsync(int id, int itemId)
    {
        var record = await _context.ReceivingRecords.FindAsync(id);
        if (record is null)
        {
            return NotFound();
        }

        if (record.Status != ReceivingStatus.Draft)
        {
            TempData["ErrorMessage"] = "Posted receiving records cannot be edited.";
            return RedirectToPage(new { id });
        }

        var item = await _context.ReceivingRecordItems.FirstOrDefaultAsync(line => line.Id == itemId && line.ReceivingRecordId == id);
        if (item is not null)
        {
            _context.ReceivingRecordItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPostReceivingAsync(int id)
    {
        var errors = await _inventoryService.PostReceivingAsync(id, User.Identity?.Name ?? "System");
        if (errors.Count > 0)
        {
            TempData["ErrorMessage"] = string.Join(" ", errors);
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var record = await _context.ReceivingRecords.FindAsync(id);
        if (record is null)
        {
            return NotFound();
        }

        if (record.Status != ReceivingStatus.Draft)
        {
            TempData["ErrorMessage"] = "Only draft receiving records can be cancelled.";
            return RedirectToPage(new { id });
        }

        record.Status = ReceivingStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var record = await _context.ReceivingRecords
            .AsNoTracking()
            .Include(receiving => receiving.PurchaseOrder)
            .Include(receiving => receiving.Supplier)
            .Include(receiving => receiving.Items)
                .ThenInclude(item => item.InventoryItem)
            .FirstOrDefaultAsync(receiving => receiving.Id == id);

        if (record is null)
        {
            return false;
        }

        ReceivingRecord = record;
        await LoadOptionsAsync();
        return true;
    }

    private async Task LoadOptionsAsync()
    {
        IQueryable<InventoryItem> query = _context.InventoryItems.AsNoTracking().Where(item => item.IsActive);

        if (ReceivingRecord.PurchaseOrderId is not null)
        {
            var orderItemIds = await _context.PurchaseOrderItems
                .Where(item => item.PurchaseOrderId == ReceivingRecord.PurchaseOrderId)
                .Select(item => item.InventoryItemId)
                .Distinct()
                .ToListAsync();

            query = query.Where(item => orderItemIds.Contains(item.Id));
        }

        var items = await query
            .OrderBy(item => item.ItemCode)
            .Select(item => new { item.Id, Name = item.ItemCode + " - " + item.ItemName })
            .ToListAsync();

        InventoryItemOptions = new SelectList(items, "Id", "Name");
    }
}
