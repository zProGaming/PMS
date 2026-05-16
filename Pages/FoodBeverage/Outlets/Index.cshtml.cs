using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.Outlets;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<Outlet> Outlets { get; set; } = new List<Outlet>();

    public async Task OnGetAsync()
    {
        Outlets = await _context.Outlets
            .AsNoTracking()
            .OrderBy(outlet => outlet.Name)
            .ToListAsync();
    }
}
