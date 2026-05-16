using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.FoodBeverage.Orders;

public class VoidItemModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public POSOrderItem OrderItem { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? orderItemId)
    {
        if (orderItemId is null)
        {
            return NotFound();
        }

        var item = await LoadOrderItemAsync(orderItemId.Value, asTracking: false);
        if (item is null)
        {
            return NotFound();
        }

        OrderItem = item;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? orderItemId)
    {
        if (orderItemId is null)
        {
            return NotFound();
        }

        var item = await LoadOrderItemAsync(orderItemId.Value, asTracking: true);
        if (item is null)
        {
            return NotFound();
        }

        if (item.POSOrder!.OrderStatus is not POSOrderStatus.Closed and not POSOrderStatus.Cancelled)
        {
            item.IsVoided = true;
            item.ItemStatus = POSOrderItemStatus.Voided;
            POSOrderTotalsCalculator.Recalculate(item.POSOrder);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Details", new { id = item.POSOrderId });
    }

    private async Task<POSOrderItem?> LoadOrderItemAsync(int id, bool asTracking)
    {
        var query = _context.POSOrderItems
            .Include(item => item.MenuItem)
            .Include(item => item.POSOrder)
                .ThenInclude(order => order!.Outlet)
            .Include(item => item.POSOrder)
                .ThenInclude(order => order!.Items)
                    .ThenInclude(orderItem => orderItem.MenuItem)
            .Where(item => item.Id == id);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.AsSplitQuery().FirstOrDefaultAsync();
    }
}
