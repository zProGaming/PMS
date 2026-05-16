using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public DateTime BusinessDate { get; set; }

    public int OpenOrders { get; set; }

    public int TablesOccupied { get; set; }

    public int RoomServiceOrders { get; set; }

    public int OrdersSentToKitchen { get; set; }

    public int PaidOrdersToday { get; set; }

    public decimal TotalFoodBeverageSalesToday { get; set; }

    public IList<POSOrder> RecentOpenOrders { get; set; } = new List<POSOrder>();

    public async Task OnGetAsync()
    {
        BusinessDate = await GetBusinessDateAsync();
        var nextBusinessDate = BusinessDate.AddDays(1);

        OpenOrders = await _context.POSOrders.CountAsync(order =>
            order.OrderStatus != POSOrderStatus.Closed &&
            order.OrderStatus != POSOrderStatus.Cancelled);

        TablesOccupied = await _context.DiningTables.CountAsync(table =>
            table.Status == DiningTableStatus.Occupied);

        RoomServiceOrders = await _context.POSOrders.CountAsync(order =>
            order.OrderType == POSOrderType.RoomService &&
            order.OrderStatus != POSOrderStatus.Closed &&
            order.OrderStatus != POSOrderStatus.Cancelled);

        OrdersSentToKitchen = await _context.POSOrders.CountAsync(order =>
            order.OrderStatus == POSOrderStatus.SentToKitchen);

        PaidOrdersToday = await _context.POSOrders.CountAsync(order =>
            order.ClosedAt >= BusinessDate &&
            order.ClosedAt < nextBusinessDate &&
            (order.PaymentStatus == POSPaymentStatus.Paid ||
             order.PaymentStatus == POSPaymentStatus.ChargedToRoom));

        TotalFoodBeverageSalesToday = await _context.POSOrders
            .Where(order =>
                order.ClosedAt >= BusinessDate &&
                order.ClosedAt < nextBusinessDate &&
                (order.PaymentStatus == POSPaymentStatus.Paid ||
                 order.PaymentStatus == POSPaymentStatus.ChargedToRoom))
            .SumAsync(order => order.TotalAmount);

        RecentOpenOrders = await _context.POSOrders
            .Include(order => order.Outlet)
            .Include(order => order.DiningTable)
            .AsNoTracking()
            .Where(order =>
                order.OrderStatus != POSOrderStatus.Closed &&
                order.OrderStatus != POSOrderStatus.Cancelled)
            .OrderByDescending(order => order.OrderDate)
            .Take(10)
            .ToListAsync();
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }
}
