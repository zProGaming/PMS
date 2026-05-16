using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Reports;

public class OccupancyModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public DateTime BusinessDate { get; set; }

    public int TotalRooms { get; set; }

    public int OccupiedRooms { get; set; }

    public int AvailableRooms { get; set; }

    public int DirtyRooms { get; set; }

    public int OutOfOrderRooms { get; set; }

    public decimal OccupancyPercentage { get; set; }

    public async Task OnGetAsync()
    {
        BusinessDate = await GetBusinessDateAsync();

        TotalRooms = await _context.Rooms.CountAsync(room => room.IsActive);
        OccupiedRooms = await CountRoomsByStatusAsync(RoomStatus.Occupied);
        AvailableRooms = await CountRoomsByStatusAsync(RoomStatus.Available);
        DirtyRooms = await CountRoomsByStatusAsync(RoomStatus.Dirty);
        OutOfOrderRooms = await CountRoomsByStatusAsync(RoomStatus.OutOfOrder);

        OccupancyPercentage = TotalRooms == 0
            ? 0
            : (decimal)OccupiedRooms / TotalRooms * 100;
    }

    private async Task<int> CountRoomsByStatusAsync(RoomStatus status)
    {
        return await _context.Rooms.CountAsync(room => room.IsActive && room.Status == status);
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }
}
