using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.MenuItems;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public MenuItem MenuItem { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await _context.MenuItems
            .Include(item => item.MenuCategory)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        MenuItem = item;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await _context.MenuItems.FindAsync(id);
        if (item is not null)
        {
            item.IsAvailable = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
