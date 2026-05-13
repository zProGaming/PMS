using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Guests;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public Guest Guest { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var guest = await _context.Guests
            .AsNoTracking()
            .FirstOrDefaultAsync(guest => guest.Id == id);

        if (guest is null)
        {
            return NotFound();
        }

        Guest = guest;
        return Page();
    }
}
