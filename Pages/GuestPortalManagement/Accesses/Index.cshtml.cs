using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.GuestPortal;

namespace Vantage.PMS.Pages.GuestPortalManagement.Accesses;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<GuestPortalAccess> Accesses { get; set; } = new List<GuestPortalAccess>();

    public async Task OnGetAsync()
    {
        Accesses = await _context.GuestPortalAccesses
            .Include(access => access.Reservation)
            .Include(access => access.BookingRequest)
            .AsNoTracking()
            .OrderByDescending(access => access.CreatedAt)
            .ToListAsync();
    }
}
