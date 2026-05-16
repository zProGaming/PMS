using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.BookingManagement.Requests;

public class DetailsModel(
    ApplicationDbContext context,
    BookingEngineService bookingEngineService,
    BookingNotificationService bookingNotificationService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly BookingEngineService _bookingEngineService = bookingEngineService;
    private readonly BookingNotificationService _bookingNotificationService = bookingNotificationService;

    public BookingRequest BookingRequest { get; set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var bookingRequest = await LoadBookingRequestAsync(id);
        if (bookingRequest is null)
        {
            return NotFound();
        }

        BookingRequest = bookingRequest;
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var bookingRequest = await _context.BookingRequests.FindAsync(id);
        if (bookingRequest is null)
        {
            return NotFound();
        }

        if (bookingRequest.BookingStatus != BookingRequestStatus.ConvertedToReservation &&
            bookingRequest.BookingStatus != BookingRequestStatus.Cancelled)
        {
            bookingRequest.BookingStatus = BookingRequestStatus.Confirmed;
            bookingRequest.ConfirmedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _bookingNotificationService.SendBookingConfirmedAsync(bookingRequest);
            StatusMessage = "Booking request confirmed.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var bookingRequest = await _context.BookingRequests.FindAsync(id);
        if (bookingRequest is null)
        {
            return NotFound();
        }

        if (bookingRequest.BookingStatus != BookingRequestStatus.ConvertedToReservation)
        {
            bookingRequest.BookingStatus = BookingRequestStatus.Cancelled;
            bookingRequest.CancelledAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _bookingNotificationService.SendBookingCancelledAsync(bookingRequest);
            StatusMessage = "Booking request cancelled.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostConvertAsync(int id)
    {
        var result = await _bookingEngineService.ConvertToReservationAsync(id);
        StatusMessage = result.Message;
        return RedirectToPage(new { id });
    }

    private async Task<BookingRequest?> LoadBookingRequestAsync(int id)
    {
        return await _context.BookingRequests
            .Include(request => request.RoomType)
            .Include(request => request.RatePlan)
            .Include(request => request.PromotionCode)
            .Include(request => request.Reservation)
            .Include(request => request.AddOns)
                .ThenInclude(addOn => addOn.BookingAddOn)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(request => request.Id == id);
    }
}
