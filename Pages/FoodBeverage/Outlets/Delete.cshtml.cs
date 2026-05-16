using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.Outlets;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public Outlet Outlet { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var outlet = await _context.Outlets
            .AsNoTracking()
            .FirstOrDefaultAsync(outlet => outlet.Id == id);

        if (outlet is null)
        {
            return NotFound();
        }

        Outlet = outlet;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var outlet = await _context.Outlets.FindAsync(id);
        if (outlet is not null)
        {
            outlet.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
