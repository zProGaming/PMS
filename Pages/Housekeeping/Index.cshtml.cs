using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Housekeeping;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public int VacantDirtyRooms { get; set; }

    public int OccupiedDirtyRooms { get; set; }

    public int CleanRooms { get; set; }

    public int InspectedRooms { get; set; }

    public int OutOfOrderRooms { get; set; }

    public int MaintenanceRooms { get; set; }

    public IList<Room> Rooms { get; set; } = new List<Room>();

    public async Task OnGetAsync()
    {
        var checkedInRoomIds = await _context.Reservations
            .Where(reservation => reservation.Status == ReservationStatus.CheckedIn && reservation.RoomId != null)
            .Select(reservation => reservation.RoomId!.Value)
            .ToListAsync();

        VacantDirtyRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive &&
            room.Status == RoomStatus.Dirty &&
            !checkedInRoomIds.Contains(room.Id));

        OccupiedDirtyRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive &&
            room.Status == RoomStatus.Dirty &&
            checkedInRoomIds.Contains(room.Id));

        CleanRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive && room.Status == RoomStatus.Clean);

        InspectedRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive && room.Status == RoomStatus.Inspected);

        OutOfOrderRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive && room.Status == RoomStatus.OutOfOrder);

        MaintenanceRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive && room.Status == RoomStatus.Maintenance);

        Rooms = await _context.Rooms
            .Include(room => room.Property)
            .Include(room => room.RoomType)
            .AsNoTracking()
            .Where(room => room.IsActive)
            .OrderBy(room => room.Property!.Name)
            .ThenBy(room => room.RoomNumber)
            .ToListAsync();
    }
}
