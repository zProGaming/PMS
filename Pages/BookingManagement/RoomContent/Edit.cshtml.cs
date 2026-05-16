using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement.RoomContent;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public BookingEngineRoomContent RoomContent { get; set; } = default!;

    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var roomContent = await _context.BookingEngineRoomContents.FindAsync(id);
        if (roomContent is null)
        {
            return NotFound();
        }

        RoomContent = roomContent;
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        _context.Attach(RoomContent).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    private async Task LoadOptionsAsync()
    {
        var roomTypes = await _context.RoomTypes
            .AsNoTracking()
            .Where(roomType => roomType.IsActive)
            .OrderBy(roomType => roomType.Code)
            .Select(roomType => new { roomType.Id, Name = roomType.Code + " - " + roomType.Name })
            .ToListAsync();

        RoomTypeOptions = new SelectList(roomTypes, "Id", "Name", RoomContent.RoomTypeId);
    }
}
