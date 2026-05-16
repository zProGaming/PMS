using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.Booking;

public class ConfirmationModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public BookingRequest? BookingRequest { get; set; }

    public async Task OnGetAsync(string reference)
    {
        BookingRequest = await _context.BookingRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.BookingReference == reference);
    }
}
