using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.SeasonalRates;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    public SeasonalRate SeasonalRate { get; set; } = default!;
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null) return NotFound();
        var rate = await _context.SeasonalRates.Include(rate => rate.RatePlan).Include(rate => rate.RoomType).AsNoTracking().FirstOrDefaultAsync(rate => rate.Id == id);
        if (rate is null) return NotFound();
        SeasonalRate = rate;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null) return NotFound();
        var rate = await _context.SeasonalRates.FindAsync(id);
        if (rate is not null)
        {
            _context.SeasonalRates.Remove(rate);
            await _context.SaveChangesAsync();
        }
        return RedirectToPage("./Index");
    }
}
