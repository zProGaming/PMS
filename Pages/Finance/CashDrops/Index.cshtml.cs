using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.Finance.CashDrops;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<CashDrop> CashDrops { get; set; } = new List<CashDrop>();

    public async Task OnGetAsync()
    {
        CashDrops = await _context.CashDrops
            .AsNoTracking()
            .Include(drop => drop.CashierShift)
            .OrderByDescending(drop => drop.DropDate)
            .Take(200)
            .ToListAsync();
    }
}
