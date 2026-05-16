using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Purchasing.PurchaseOrders;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    public async Task OnGetAsync()
    {
        PurchaseOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(order => order.Supplier)
            .Include(order => order.PurchaseRequest)
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.Id)
            .ToListAsync();
    }
}
