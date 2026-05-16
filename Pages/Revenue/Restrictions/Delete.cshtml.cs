using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.Restrictions;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    public RateRestriction RateRestriction { get; set; } = default!;
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null) return NotFound();
        var restriction = await _context.RateRestrictions.Include(restriction => restriction.RatePlan).Include(restriction => restriction.RoomType).AsNoTracking().FirstOrDefaultAsync(restriction => restriction.Id == id);
        if (restriction is null) return NotFound();
        RateRestriction = restriction;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null) return NotFound();
        var restriction = await _context.RateRestrictions.FindAsync(id);
        if (restriction is not null)
        {
            _context.RateRestrictions.Remove(restriction);
            await _context.SaveChangesAsync();
        }
        return RedirectToPage("./Index");
    }
}
