using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement.RoomContent;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public BookingEngineRoomContent RoomContent { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var roomContent = await _context.BookingEngineRoomContents
            .Include(content => content.RoomType)
            .FirstOrDefaultAsync(content => content.Id == id);

        if (roomContent is null)
        {
            return NotFound();
        }

        RoomContent = roomContent;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var roomContent = await _context.BookingEngineRoomContents.FindAsync(id);
        if (roomContent is not null)
        {
            _context.BookingEngineRoomContents.Remove(roomContent);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
