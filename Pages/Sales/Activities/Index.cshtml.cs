using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Activities;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<SalesActivity> SalesActivities { get; set; } = new List<SalesActivity>();

    public async Task OnGetAsync()
    {
        SalesActivities = await _context.SalesActivities
            .Include(activity => activity.SalesAccount)
            .Include(activity => activity.SalesLead)
            .AsNoTracking()
            .OrderByDescending(activity => activity.ActivityDate)
            .Take(200)
            .ToListAsync();
    }
}
