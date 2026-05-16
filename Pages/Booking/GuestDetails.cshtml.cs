using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Services;
using Vantage.PMS.ViewModels.Booking;

namespace Vantage.PMS.Pages.Booking;

public class GuestDetailsModel(ApplicationDbContext context, BookingEngineService bookingEngineService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly BookingEngineService _bookingEngineService = bookingEngineService;

    [BindProperty(SupportsGet = true)]
    public BookingStayInput BookingInput { get; set; } = new();

    [BindProperty]
    public BookingGuestForm GuestInput { get; set; } = new();

    [BindProperty]
    public List<BookingAddOnSelectionForm> AddOnSelections { get; set; } = new();

    [BindProperty]
    public bool GuestAcceptedTerms { get; set; }

    public IList<BookingAddOn> AvailableAddOns { get; set; } = new List<BookingAddOn>();
    public BookingEngineService.BookingQuoteResult QuoteResult { get; set; } = new(null, new List<string>());

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAddOnsAsync();
        QuoteResult = await _bookingEngineService.BuildQuoteAsync(ToQuoteInput());
        return Page();
    }

    private async Task LoadAddOnsAsync()
    {
        AvailableAddOns = await _context.BookingAddOns
            .AsNoTracking()
            .Where(addOn => addOn.IsActive)
            .OrderBy(addOn => addOn.Name)
            .ToListAsync();

        AddOnSelections = AvailableAddOns
            .Select(addOn => new BookingAddOnSelectionForm { BookingAddOnId = addOn.Id, Quantity = 0 })
            .ToList();
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
