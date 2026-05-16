using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverageKitchen.KitchenStations;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<KitchenStation> KitchenStations { get; set; } = new List<KitchenStation>();

    public async Task OnGetAsync()
    {
        KitchenStations = await _context.KitchenStations
            .AsNoTracking()
            .OrderBy(station => station.Name)
            .ToListAsync();
    }
}
