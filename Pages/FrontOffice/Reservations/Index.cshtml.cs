using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Reservations;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<Reservation> Reservations { get; set; } = new List<Reservation>();

    public async Task OnGetAsync()
    {
        Reservations = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .Include(reservation => reservation.RoomType)
            .Include(reservation => reservation.Property)
            .AsNoTracking()
            .OrderByDescending(reservation => reservation.ArrivalDate)
            .ThenBy(reservation => reservation.Guest!.LastName)
            .ToListAsync();
    }
}
