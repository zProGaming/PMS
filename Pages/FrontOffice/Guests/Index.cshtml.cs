using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Guests;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<Guest> Guests { get; set; } = new List<Guest>();

    public async Task OnGetAsync()
    {
        Guests = await _context.Guests
            .AsNoTracking()
            .OrderBy(guest => guest.LastName)
            .ThenBy(guest => guest.FirstName)
            .ToListAsync();
    }
}
