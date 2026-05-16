using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.DiningTables;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public DiningTable DiningTable { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var table = await _context.DiningTables
            .Include(table => table.Outlet)
            .AsNoTracking()
            .FirstOrDefaultAsync(table => table.Id == id);

        if (table is null)
        {
            return NotFound();
        }

        DiningTable = table;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var table = await _context.DiningTables.FindAsync(id);
        if (table is not null)
        {
            table.Status = DiningTableStatus.OutOfService;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
