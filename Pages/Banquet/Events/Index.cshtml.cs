using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.Events;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<BanquetEvent> BanquetEvents { get; set; } = new List<BanquetEvent>();

    public async Task OnGetAsync()
    {
        BanquetEvents = await _context.BanquetEvents
            .Include(banquetEvent => banquetEvent.FunctionRoom)
            .Include(banquetEvent => banquetEvent.SalesAccount)
            .Include(banquetEvent => banquetEvent.SalesLead)
            .Include(banquetEvent => banquetEvent.BanquetPackage)
            .AsNoTracking()
            .OrderByDescending(banquetEvent => banquetEvent.EventDate)
            .ThenBy(banquetEvent => banquetEvent.StartTime)
            .ToListAsync();
    }
}
