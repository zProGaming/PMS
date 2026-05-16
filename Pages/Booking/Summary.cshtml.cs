using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;
using Vantage.PMS.ViewModels.Booking;

namespace Vantage.PMS.Pages.Booking;

public class SummaryModel(BookingEngineService bookingEngineService) : PageModel
{
    private readonly BookingEngineService _bookingEngineService = bookingEngineService;

    [BindProperty]
    public BookingStayInput BookingInput { get; set; } = new();

    [BindProperty]
    public BookingGuestForm GuestInput { get; set; } = new();

    [BindProperty]
    public List<BookingAddOnSelectionForm> AddOnSelections { get; set; } = new();

    [BindProperty]
    public bool GuestAcceptedTerms { get; set; }

    [BindProperty]
    public bool SummaryAcceptedTerms { get; set; }

    public BookingEngineService.BookingQuoteResult QuoteResult { get; set; } = new(null, new List<string>());

    public IActionResult OnGet()
    {
        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await BuildAndValidateAsync(requireSummaryAcceptance: false);
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        await BuildAndValidateAsync(requireSummaryAcceptance: true);
        if (!ModelState.IsValid || QuoteResult.Quote is null || QuoteResult.Errors.Count > 0)
        {
            return Page();
        }

        var bookingRequest = await _bookingEngineService.CreateBookingRequestAsync(
            QuoteResult.Quote,
            new BookingEngineService.BookingGuestInput(
                GuestInput.FirstName,
                GuestInput.LastName,
                GuestInput.Email,
                GuestInput.Phone,
                GuestInput.Address,
                GuestInput.SpecialRequests));

        return RedirectToPage("Confirmation", new { reference = bookingRequest.BookingReference });
    }

    private async Task BuildAndValidateAsync(bool requireSummaryAcceptance)
    {
        QuoteResult = await _bookingEngineService.BuildQuoteAsync(ToQuoteInput());

        if (string.IsNullOrWhiteSpace(GuestInput.FirstName))
        {
            ModelState.AddModelError("GuestInput.FirstName", "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(GuestInput.LastName))
        {
            ModelState.AddModelError("GuestInput.LastName", "Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(GuestInput.Email))
        {
            ModelState.AddModelError("GuestInput.Email", "Email is required.");
        }

        if (!GuestAcceptedTerms)
        {
            ModelState.AddModelError(string.Empty, "You must accept the booking terms before continuing.");
        }

        if (requireSummaryAcceptance && !SummaryAcceptedTerms)
        {
            ModelState.AddModelError(string.Empty, "You must confirm the terms and conditions before submitting.");
        }

        foreach (var error in QuoteResult.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }

    private BookingEngineService.BookingQuoteInput ToQuoteInput()
    {
        return new BookingEngineService.BookingQuoteInput(
            BookingInput.RoomTypeId,
            BookingInput.RatePlanId,
            BookingInput.CheckInDate,
            BookingInput.CheckOutDate,
            BookingInput.AdultCount,
            BookingInput.ChildCount,
            BookingInput.PromoCode,
            AddOnSelections.Select(addOn => new BookingEngineService.BookingAddOnSelection(addOn.BookingAddOnId, addOn.Quantity)));
    }
}
