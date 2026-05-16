using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.MenuItems;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

    public async Task OnGetAsync()
    {
        MenuItems = await _context.MenuItems
            .Include(item => item.MenuCategory)
            .Include(item => item.KitchenStation)
            .AsNoTracking()
            .OrderBy(item => item.MenuCategory!.SortOrder)
            .ThenBy(item => item.MenuCategory!.Name)
            .ThenBy(item => item.Name)
            .ToListAsync();
    }
}
