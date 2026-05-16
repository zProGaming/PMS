using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.Reports;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<InventoryItem> StockOnHand { get; set; } = new List<InventoryItem>();
    public IList<InventoryItem> LowStockItems { get; set; } = new List<InventoryItem>();
    public IList<InventoryItem> ExpiringItems { get; set; } = new List<InventoryItem>();
    public IList<PurchaseRequest> PurchaseRequests { get; set; } = new List<PurchaseRequest>();
    public IList<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public IList<ReceivingRecord> ReceivingRecords { get; set; } = new List<ReceivingRecord>();
    public IList<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var nextWeek = today.AddDays(7);

        StockOnHand = await _context.InventoryItems
            .AsNoTracking()
            .Include(item => item.InventoryCategory)
            .Where(item => item.IsActive)
            .OrderBy(item => item.ItemCode)
            .ToListAsync();

        LowStockItems = StockOnHand
            .Where(item => item.CurrentStock <= item.ReorderLevel)
            .ToList();

        ExpiringItems = StockOnHand
            .Where(item => item.IsPerishable && item.ExpiryDate is not null && item.ExpiryDate.Value.Date >= today && item.ExpiryDate.Value.Date <= nextWeek)
            .OrderBy(item => item.ExpiryDate)
            .ToList();

        PurchaseRequests = await _context.PurchaseRequests
            .AsNoTracking()
            .Include(request => request.Department)
            .Include(request => request.Items)
            .OrderByDescending(request => request.RequestDate)
            .Take(25)
            .ToListAsync();

        PurchaseOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(order => order.Supplier)
            .OrderByDescending(order => order.OrderDate)
            .Take(25)
            .ToListAsync();

        ReceivingRecords = await _context.ReceivingRecords
            .AsNoTracking()
            .Include(record => record.PurchaseOrder)
            .Include(record => record.Supplier)
            .Include(record => record.Items)
            .OrderByDescending(record => record.ReceivedDate)
            .Take(25)
            .ToListAsync();

        StockMovements = await _context.StockMovements
            .AsNoTracking()
            .Include(movement => movement.InventoryItem)
            .Include(movement => movement.Department)
            .OrderByDescending(movement => movement.MovementDate)
            .Take(50)
            .ToListAsync();
    }
}
