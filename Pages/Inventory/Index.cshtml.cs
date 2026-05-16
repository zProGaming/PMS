using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public int TotalActiveItems { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public int PurchaseRequestsPendingApproval { get; set; }
    public int PurchaseOrdersPendingApproval { get; set; }
    public int PurchaseOrdersPartiallyReceived { get; set; }
    public decimal InventoryValue { get; set; }
    public int PerishableItemsExpiringSoon { get; set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var nextWeek = today.AddDays(7);

        TotalActiveItems = await _context.InventoryItems.CountAsync(item => item.IsActive);
        LowStockItems = await _context.InventoryItems.CountAsync(item => item.IsActive && item.CurrentStock > 0 && item.CurrentStock <= item.ReorderLevel);
        OutOfStockItems = await _context.InventoryItems.CountAsync(item => item.IsActive && item.CurrentStock <= 0);
        PurchaseRequestsPendingApproval = await _context.PurchaseRequests.CountAsync(request => request.Status == PurchaseRequestStatus.Submitted);
        PurchaseOrdersPendingApproval = await _context.PurchaseOrders.CountAsync(order => order.Status == PurchaseOrderStatus.ForApproval);
        PurchaseOrdersPartiallyReceived = await _context.PurchaseOrders.CountAsync(order => order.Status == PurchaseOrderStatus.PartiallyReceived);
        InventoryValue = await _context.InventoryItems.SumAsync(item => item.CurrentStock * item.StandardCost);
        PerishableItemsExpiringSoon = await _context.InventoryItems.CountAsync(item =>
            item.IsActive &&
            item.IsPerishable &&
            item.ExpiryDate != null &&
            item.ExpiryDate.Value.Date >= today &&
            item.ExpiryDate.Value.Date <= nextWeek);
    }
}
