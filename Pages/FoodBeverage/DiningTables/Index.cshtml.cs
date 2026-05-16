using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.DiningTables;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<DiningTable> DiningTables { get; set; } = new List<DiningTable>();

    public async Task OnGetAsync()
    {
        DiningTables = await _context.DiningTables
            .Include(table => table.Outlet)
            .AsNoTracking()
            .OrderBy(table => table.Outlet!.Name)
            .ThenBy(table => table.TableName)
            .ToListAsync();
    }
}
