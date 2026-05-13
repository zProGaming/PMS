using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Admin.Rooms;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
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

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var room = await _context.Rooms.FindAsync(id);
        if (room is not null)
        {
            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
