using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement.Requests;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();

    public async Task OnGetAsync()
    {
        BookingRequests = await _context.BookingRequests
            .Include(request => request.RoomType)
            .AsNoTracking()
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync();
    }
}
