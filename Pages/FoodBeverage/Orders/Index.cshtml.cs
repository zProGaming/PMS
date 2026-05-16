using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.Orders;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<POSOrder> Orders { get; set; } = new List<POSOrder>();

    public async Task OnGetAsync()
    {
        Orders = await _context.POSOrders
            .Include(order => order.Outlet)
            .Include(order => order.DiningTable)
            .Include(order => order.Guest)
            .AsNoTracking()
            .OrderByDescending(order => order.OrderDate)
            .Take(200)
            .ToListAsync();
    }
}
