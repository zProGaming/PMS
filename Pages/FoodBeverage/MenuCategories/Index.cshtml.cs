using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.MenuCategories;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<MenuCategory> MenuCategories { get; set; } = new List<MenuCategory>();

    public async Task OnGetAsync()
    {
        MenuCategories = await _context.MenuCategories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToListAsync();
    }
}
