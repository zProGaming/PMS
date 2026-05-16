using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Activities;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public SalesActivity SalesActivity { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var activity = await _context.SalesActivities
            .Include(activity => activity.SalesAccount)
            .Include(activity => activity.SalesLead)
            .AsNoTracking()
            .FirstOrDefaultAsync(activity => activity.Id == id);

        if (activity is null)
        {
            return NotFound();
        }

        SalesActivity = activity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var activity = await _context.SalesActivities.FindAsync(id);
        if (activity is not null)
        {
            _context.SalesActivities.Remove(activity);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
