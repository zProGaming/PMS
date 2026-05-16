using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.Restrictions;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    public IList<RateRestriction> RateRestrictions { get; set; } = new List<RateRestriction>();

    public async Task OnGetAsync()
    {
        RateRestrictions = await _context.RateRestrictions
            .Include(restriction => restriction.RatePlan)
            .Include(restriction => restriction.RoomType)
            .AsNoTracking()
            .OrderBy(restriction => restriction.RestrictionDate)
            .ToListAsync();
    }
}
