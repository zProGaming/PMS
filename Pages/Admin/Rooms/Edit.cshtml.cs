using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Admin.Rooms;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Room Room { get; set; } = default!;

    public SelectList PropertyOptions { get; set; } = default!;

    public SelectList RoomTypeOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> RoomStatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var room = await _context.Rooms.FindAsync(id);
        if (room is null)
        {
            return NotFound();
        }

        Room = room;
        await LoadSelectListsAsync(Room.PropertyId, Room.RoomTypeId, Room.Status);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(Room.PropertyId, Room.RoomTypeId, Room.Status);
            return Page();
        }

        _context.Attach(Room).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await RoomExistsAsync(Room.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToPage("./Index");
    }

    private async Task LoadSelectListsAsync(object? selectedProperty = null, object? selectedRoomType = null, RoomStatus selectedStatus = RoomStatus.Available)
    {
        var properties = await _context.Properties
            .AsNoTracking()
            .OrderBy(property => property.Name)
            .Select(property => new { property.Id, property.Name })
            .ToListAsync();

        var roomTypes = await _context.RoomTypes
            .Include(roomType => roomType.Property)
            .AsNoTracking()
            .OrderBy(roomType => roomType.Property!.Name)
            .ThenBy(roomType => roomType.Name)
            .ToListAsync();

        PropertyOptions = new SelectList(properties, "Id", "Name", selectedProperty);
        RoomTypeOptions = new SelectList(
            roomTypes.Select(roomType => new { roomType.Id, Name = $"{roomType.Property?.Name} - {roomType.Name}" }),
            "Id",
            "Name",
            selectedRoomType);
        RoomStatusOptions = BuildRoomStatusOptions(selectedStatus);
    }

    private Task<bool> RoomExistsAsync(int id)
    {
        return _context.Rooms.AnyAsync(room => room.Id == id);
    }

    private static IEnumerable<SelectListItem> BuildRoomStatusOptions(RoomStatus selectedStatus)
    {
        return Enum.GetValues<RoomStatus>()
            .Select(status => new SelectListItem
            {
                Value = status.ToString(),
                Text = status.ToString(),
                Selected = status == selectedStatus
            });
    }
}
