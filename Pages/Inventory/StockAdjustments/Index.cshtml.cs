using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.StockAdjustments;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();

    public async Task OnGetAsync()
    {
        StockAdjustments = await _context.StockAdjustments
            .AsNoTracking()
            .Include(adjustment => adjustment.Items)
            .OrderByDescending(adjustment => adjustment.AdjustmentDate)
            .ThenByDescending(adjustment => adjustment.Id)
            .ToListAsync();
    }
}
