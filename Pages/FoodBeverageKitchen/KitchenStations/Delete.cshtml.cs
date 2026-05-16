using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverageKitchen.KitchenStations;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public KitchenStation KitchenStation { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var station = await _context.KitchenStations
            .AsNoTracking()
            .FirstOrDefaultAsync(station => station.Id == id);

        if (station is null)
        {
            return NotFound();
        }

        KitchenStation = station;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var station = await _context.KitchenStations.FindAsync(id);
        if (station is not null)
        {
            _context.KitchenStations.Remove(station);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
