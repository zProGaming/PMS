using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Guests;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
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

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var guest = await _context.Guests.FindAsync(id);
        if (guest is not null)
        {
            _context.Guests.Remove(guest);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
