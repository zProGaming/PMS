using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.Categories;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    public IList<InventoryCategory> Categories { get; set; } = new List<InventoryCategory>();
    public async Task OnGetAsync() => Categories = await _context.InventoryCategories.AsNoTracking().OrderBy(category => category.Name).ToListAsync();
}
