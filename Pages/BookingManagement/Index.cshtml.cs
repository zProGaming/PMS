using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public int PendingRequests { get; set; }
    public int ConfirmedRequests { get; set; }
    public int ConvertedThisMonth { get; set; }
    public int CancelledThisMonth { get; set; }
    public decimal EstimatedRevenueThisMonth { get; set; }
    public IList<RoomTypeRequestCount> MostRequestedRoomTypes { get; set; } = new List<RoomTypeRequestCount>();

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        PendingRequests = await _context.BookingRequests.CountAsync(request => request.BookingStatus == BookingRequestStatus.Pending);
        ConfirmedRequests = await _context.BookingRequests.CountAsync(request => request.BookingStatus == BookingRequestStatus.Confirmed);
        ConvertedThisMonth = await _context.BookingRequests.CountAsync(request =>
            request.BookingStatus == BookingRequestStatus.ConvertedToReservation &&
            request.ConfirmedAt >= monthStart &&
            request.ConfirmedAt < nextMonth);
        CancelledThisMonth = await _context.BookingRequests.CountAsync(request =>
            request.BookingStatus == BookingRequestStatus.Cancelled &&
            request.CancelledAt >= monthStart &&
            request.CancelledAt < nextMonth);
        EstimatedRevenueThisMonth = await _context.BookingRequests
            .Where(request =>
                request.CreatedAt >= monthStart &&
                request.CreatedAt < nextMonth &&
                request.BookingStatus != BookingRequestStatus.Cancelled)
            .SumAsync(request => request.TotalRoomAmount);

        var requests = await _context.BookingRequests
            .Include(request => request.RoomType)
            .AsNoTracking()
            .ToListAsync();

        MostRequestedRoomTypes = requests
            .GroupBy(request => request.RoomType?.Name ?? "Unknown")
            .Select(group => new RoomTypeRequestCount(group.Key, group.Count()))
            .OrderByDescending(row => row.RequestCount)
            .Take(5)
            .ToList();
    }

    public record RoomTypeRequestCount(string RoomTypeName, int RequestCount);
}
