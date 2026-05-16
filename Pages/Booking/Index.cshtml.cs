using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Services;
using Vantage.PMS.ViewModels.Booking;

namespace Vantage.PMS.Pages.Booking;

public class IndexModel(BookingEngineService bookingEngineService) : PageModel
{
    private readonly BookingEngineService _bookingEngineService = bookingEngineService;

    [BindProperty]
    public BookingStayInput Input { get; set; } = new();

    public BookingEngineSetting Setting { get; set; } = new();

    public async Task OnGetAsync()
    {
        Setting = await _bookingEngineService.GetSettingsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Setting = await _bookingEngineService.GetSettingsAsync();

        ValidateSearch();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        return RedirectToPage("Rooms", new
        {
            checkInDate = Input.CheckInDate.ToString("yyyy-MM-dd"),
            checkOutDate = Input.CheckOutDate.ToString("yyyy-MM-dd"),
            adultCount = Input.AdultCount,
            childCount = Input.ChildCount,
            promoCode = Input.PromoCode
        });
    }

    private void ValidateSearch()
    {
        if (Input.CheckInDate.Date >= Input.CheckOutDate.Date)
        {
            ModelState.AddModelError(string.Empty, "Check-out date must be after check-in date.");
        }

        if ((Input.CheckOutDate.Date - Input.CheckInDate.Date).Days < 1)
        {
            ModelState.AddModelError(string.Empty, "Stay must be at least 1 night.");
        }

        if (Input.AdultCount < 1)
        {
            ModelState.AddModelError("Input.AdultCount", "At least one adult is required.");
        }
    }
}
