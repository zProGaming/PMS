using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Housekeeping.Rooms;

public class UpdateStatusModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Room Room { get; set; } = default!;

    [BindProperty]
    public RoomStatus TargetStatus { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var room = await LoadRoomAsync(id.Value, asTracking: false);
        if (room is null)
        {
            return NotFound();
        }

        Room = room;
        Notes = room.StatusNotes;
        LoadStatusOptions(room.Status);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var room = await LoadRoomAsync(id.Value, asTracking: true);
        if (room is null)
        {
            return NotFound();
        }

        Room = room;
        ValidateStatusTransition(room.Status, TargetStatus);

        if (!ModelState.IsValid)
        {
            LoadStatusOptions(room.Status);
            return Page();
        }

        room.Status = TargetStatus;
        room.StatusNotes = TargetStatus == RoomStatus.OutOfOrder
            ? Notes
            : string.IsNullOrWhiteSpace(Notes) ? null : Notes;

        await _context.SaveChangesAsync();

        return RedirectToPage("/Housekeeping/Index");
    }

    private async Task<Room?> LoadRoomAsync(int id, bool asTracking)
    {
        var query = _context.Rooms
            .Include(room => room.Property)
            .Include(room => room.RoomType)
            .Where(room => room.Id == id);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private void LoadStatusOptions(RoomStatus currentStatus)
    {
        StatusOptions = GetAllowedStatuses(currentStatus)
            .Select(status => new SelectListItem
            {
                Value = status.ToString(),
                Text = status.ToString(),
                Selected = status == TargetStatus
            });
    }

    private void ValidateStatusTransition(RoomStatus currentStatus, RoomStatus targetStatus)
    {
        if (!GetAllowedStatuses(currentStatus).Contains(targetStatus))
        {
            ModelState.AddModelError(nameof(TargetStatus), $"Cannot change room from {currentStatus} to {targetStatus}.");
        }

        if (targetStatus == RoomStatus.OutOfOrder && string.IsNullOrWhiteSpace(Notes))
        {
            ModelState.AddModelError(nameof(Notes), "Notes are required when placing a room out of order.");
        }
    }

    private static IEnumerable<RoomStatus> GetAllowedStatuses(RoomStatus currentStatus)
    {
        var statuses = currentStatus switch
        {
            RoomStatus.Dirty => new[] { RoomStatus.Clean },
            RoomStatus.Clean => new[] { RoomStatus.Inspected },
            RoomStatus.Inspected => new[] { RoomStatus.Available },
            RoomStatus.Available => new[] { RoomStatus.Maintenance },
            RoomStatus.Maintenance => new[] { RoomStatus.Available },
            _ => Array.Empty<RoomStatus>()
        };

        return currentStatus == RoomStatus.OutOfOrder
            ? statuses
            : statuses.Append(RoomStatus.OutOfOrder);
    }
}
