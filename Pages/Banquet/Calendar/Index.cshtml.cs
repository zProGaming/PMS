using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.Calendar;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<BanquetEvent> BanquetEvents { get; set; } = new List<BanquetEvent>();

    public async Task OnGetAsync()
    {
        var startDate = DateTime.Today.AddDays(-7);

        BanquetEvents = await _context.BanquetEvents
            .Include(banquetEvent => banquetEvent.FunctionRoom)
            .AsNoTracking()
            .Where(banquetEvent => banquetEvent.EventDate >= startDate)
            .OrderBy(banquetEvent => banquetEvent.EventDate)
            .ThenBy(banquetEvent => banquetEvent.StartTime)
            .ToListAsync();
    }
}
