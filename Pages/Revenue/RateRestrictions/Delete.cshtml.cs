using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RateRestrictions;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public RateRestriction RateRestriction { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var rateRestriction = await _context.RateRestrictions
            .Include(r => r.RatePlan)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rateRestriction == null)
        {
            return NotFound();
        }

        RateRestriction = rateRestriction;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var rateRestriction = await _context.RateRestrictions.FindAsync(id);
        if (rateRestriction != null)
        {
            _context.RateRestrictions.Remove(rateRestriction);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
