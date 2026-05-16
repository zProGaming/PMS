using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.Items;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public InventoryItem InventoryItem { get; set; } = new();
    public IList<StockMovement> RecentMovements { get; set; } = new List<StockMovement>();
    public IList<PurchaseOrderItem> PurchaseHistory { get; set; } = new List<PurchaseOrderItem>();
    public IList<ReceivingRecordItem> ReceivingHistory { get; set; } = new List<ReceivingRecordItem>();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _context.InventoryItems
            .AsNoTracking()
            .Include(inventoryItem => inventoryItem.InventoryCategory)
            .FirstOrDefaultAsync(inventoryItem => inventoryItem.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        InventoryItem = item;
        RecentMovements = await _context.StockMovements
            .AsNoTracking()
            .Include(movement => movement.Department)
            .Where(movement => movement.InventoryItemId == id)
            .OrderByDescending(movement => movement.MovementDate)
            .Take(10)
            .ToListAsync();

        PurchaseHistory = await _context.PurchaseOrderItems
            .AsNoTracking()
            .Include(orderItem => orderItem.PurchaseOrder)
                .ThenInclude(order => order!.Supplier)
            .Where(orderItem => orderItem.InventoryItemId == id)
            .OrderByDescending(orderItem => orderItem.PurchaseOrder!.OrderDate)
            .Take(10)
            .ToListAsync();

        ReceivingHistory = await _context.ReceivingRecordItems
            .AsNoTracking()
            .Include(receivingItem => receivingItem.ReceivingRecord)
                .ThenInclude(record => record!.Supplier)
            .Where(receivingItem => receivingItem.InventoryItemId == id)
            .OrderByDescending(receivingItem => receivingItem.ReceivingRecord!.ReceivedDate)
            .Take(10)
            .ToListAsync();

        return Page();
    }
}
