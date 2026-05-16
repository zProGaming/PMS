using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RatePlans;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<RatePlan> RatePlans { get; set; } = new List<RatePlan>();

    public async Task OnGetAsync()
    {
        RatePlans = await _context.RatePlans
            .AsNoTracking()
            .OrderBy(ratePlan => ratePlan.Code)
            .ToListAsync();
    }
}
