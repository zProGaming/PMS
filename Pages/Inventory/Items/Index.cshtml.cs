using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.Items;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<InventoryItem> Items { get; set; } = new List<InventoryItem>();

    public async Task OnGetAsync()
    {
        Items = await _context.InventoryItems
            .AsNoTracking()
            .Include(item => item.InventoryCategory)
            .OrderBy(item => item.ItemCode)
            .ToListAsync();
    }
}
