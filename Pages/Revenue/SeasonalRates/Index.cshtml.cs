using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.SeasonalRates;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<SeasonalRate> SeasonalRates { get; set; } = new List<SeasonalRate>();

    public async Task OnGetAsync()
    {
        SeasonalRates = await _context.SeasonalRates
            .Include(rate => rate.RatePlan)
            .Include(rate => rate.RoomType)
            .AsNoTracking()
            .OrderBy(rate => rate.StartDate)
            .ThenBy(rate => rate.SeasonName)
            .ToListAsync();
    }
}
