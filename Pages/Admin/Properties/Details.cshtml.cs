using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Properties;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public Property Property { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var property = await _context.Properties
            .Include(property => property.Hotel)
            .AsNoTracking()
            .FirstOrDefaultAsync(property => property.Id == id);

        if (property is null)
        {
            return NotFound();
        }

        Property = property;
        return Page();
    }
}
