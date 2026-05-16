using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement.RoomContent;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<BookingEngineRoomContent> RoomContents { get; set; } = new List<BookingEngineRoomContent>();

    public async Task OnGetAsync()
    {
        RoomContents = await _context.BookingEngineRoomContents
            .Include(content => content.RoomType)
            .AsNoTracking()
            .OrderBy(content => content.SortOrder)
            .ThenBy(content => content.DisplayName)
            .ToListAsync();
    }
}
