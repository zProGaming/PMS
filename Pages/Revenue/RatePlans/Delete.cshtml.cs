using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RatePlans;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public RatePlan RatePlan { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var ratePlan = await _context.RatePlans.AsNoTracking().FirstOrDefaultAsync(ratePlan => ratePlan.Id == id);
        if (ratePlan is null)
        {
            return NotFound();
        }

        RatePlan = ratePlan;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var ratePlan = await _context.RatePlans.FindAsync(id);
        if (ratePlan is not null)
        {
            _context.RatePlans.Remove(ratePlan);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
