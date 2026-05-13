using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Hotels;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<Hotel> Hotels { get; set; } = new List<Hotel>();

    public async Task OnGetAsync()
    {
        Hotels = await _context.Hotels
            .AsNoTracking()
            .OrderBy(hotel => hotel.Name)
            .ToListAsync();
    }
}
