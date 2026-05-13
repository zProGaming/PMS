using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Admin.Rooms;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public Room Room { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var room = await _context.Rooms
            .Include(room => room.Property)
            .Include(room => room.RoomType)
            .AsNoTracking()
            .FirstOrDefaultAsync(room => room.Id == id);

        if (room is null)
        {
            return NotFound();
        }

        Room = room;
        return Page();
    }
}
