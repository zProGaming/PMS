using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement.RoomContent;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public BookingEngineRoomContent RoomContent { get; set; } = new() { IsVisible = true };

    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        _context.BookingEngineRoomContents.Add(RoomContent);
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
