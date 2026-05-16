using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RoomTypeRates;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    public RoomTypeRate RoomTypeRate { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null) return NotFound();
        var rate = await _context.RoomTypeRates.Include(rate => rate.RatePlan).Include(rate => rate.RoomType).AsNoTracking().FirstOrDefaultAsync(rate => rate.Id == id);
        if (rate is null) return NotFound();
        RoomTypeRate = rate;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null) return NotFound();
        var rate = await _context.RoomTypeRates.FindAsync(id);
        if (rate is not null)
        {
            _context.RoomTypeRates.Remove(rate);
            await _context.SaveChangesAsync();
        }
        return RedirectToPage("./Index");
    }
}
