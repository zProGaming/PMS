using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.Orders;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public POSOrder Order { get; set; } = default!;

    public bool CanEditOrder => Order.OrderStatus != POSOrderStatus.Closed &&
                                Order.OrderStatus != POSOrderStatus.Cancelled;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var order = await LoadOrderAsync(id.Value);
        if (order is null)
        {
            return NotFound();
        }

        Order = order;
        return Page();
    }

    public async Task<IActionResult> OnPostSendToKitchenAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var order = await _context.POSOrders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        if (order.OrderStatus == POSOrderStatus.Open)
        {
            var activeItems = order.Items
                .Where(item => !item.IsVoided &&
                               item.ItemStatus != POSOrderItemStatus.Cancelled &&
                               item.ItemStatus != POSOrderItemStatus.Voided)
                .ToList();

            if (activeItems.Count == 0)
            {
                return RedirectToPage("./Details", new { id = order.Id });
            }

            var sentAt = DateTime.Now;
            order.OrderStatus = POSOrderStatus.SentToKitchen;

            foreach (var item in activeItems.Where(item => item.SentToKitchenAt is null))
            {
                item.SentToKitchenAt = sentAt;
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Details", new { id = order.Id });
    }

    private async Task<POSOrder?> LoadOrderAsync(int id)
    {
        return await _context.POSOrders
            .Include(order => order.Outlet)
            .Include(order => order.DiningTable)
            .Include(order => order.Guest)
            .Include(order => order.Reservation)
                .ThenInclude(reservation => reservation!.Room)
            .Include(order => order.Items)
                .ThenInclude(item => item.MenuItem)
                    .ThenInclude(menuItem => menuItem!.MenuCategory)
            .Include(order => order.Items)
                .ThenInclude(item => item.MenuItem)
                    .ThenInclude(menuItem => menuItem!.KitchenStation)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(order => order.Id == id);
    }
}
