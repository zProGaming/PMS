using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Services;
using Vantage.PMS.ViewModels.Booking;

namespace Vantage.PMS.Pages.Booking;

public class RoomsModel(BookingEngineService bookingEngineService) : PageModel
{
    private readonly BookingEngineService _bookingEngineService = bookingEngineService;

    public BookingStayInput Input { get; set; } = new();
    public BookingEngineSetting Setting { get; set; } = new();
    public IList<BookingEngineService.BookingAvailabilityOption> AvailableRooms { get; set; } = new List<BookingEngineService.BookingAvailabilityOption>();

    public async Task OnGetAsync(DateTime checkInDate, DateTime checkOutDate, int adultCount = 1, int childCount = 0, string? promoCode = null)
    {
        Input = new BookingStayInput
        {
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            AdultCount = adultCount,
            ChildCount = childCount,
            PromoCode = promoCode
        };

        Setting = await _bookingEngineService.GetSettingsAsync();
        if (Setting.IsBookingEngineEnabled && checkOutDate.Date > checkInDate.Date)
        {
            AvailableRooms = await _bookingEngineService.SearchAvailabilityAsync(new BookingEngineService.BookingSearchCriteria(
                checkInDate,
                checkOutDate,
                adultCount,
                childCount,
                promoCode));
        }
    }
}
