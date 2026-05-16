using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RateRestrictions;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<RateRestriction> RateRestrictions { get; set; } = new List<RateRestriction>();

    public async Task OnGetAsync()
    {
        RateRestrictions = await _context.RateRestrictions
            .Include(r => r.RatePlan)
            .Include(r => r.RoomType)
            .OrderBy(r => r.RestrictionDate)
            .ThenBy(r => r.RoomType != null ? r.RoomType.Code : string.Empty)
            .ToListAsync();
    }
}
