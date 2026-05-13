using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Admin.Rooms;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<Room> Rooms { get; set; } = new List<Room>();

    public async Task OnGetAsync()
    {
        Rooms = await _context.Rooms
            .Include(room => room.Property)
            .Include(room => room.RoomType)
            .AsNoTracking()
            .OrderBy(room => room.RoomNumber)
            .ToListAsync();
    }
}
