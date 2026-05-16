using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RoomTypeRates;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<RoomTypeRate> RoomTypeRates { get; set; } = new List<RoomTypeRate>();

    public async Task OnGetAsync()
    {
        RoomTypeRates = await _context.RoomTypeRates
            .Include(rate => rate.RatePlan)
            .Include(rate => rate.RoomType)
            .AsNoTracking()
            .OrderBy(rate => rate.RatePlan!.Code)
            .ThenBy(rate => rate.RoomType!.Code)
            .ToListAsync();
    }
}
