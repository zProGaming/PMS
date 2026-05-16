using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.GuestPortal;

public class ExpressCheckoutModel(
    ApplicationDbContext context,
    GuestPortalService guestPortalService,
    GuestPortalNotificationService notificationService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly GuestPortalService _guestPortalService = guestPortalService;
    private readonly GuestPortalNotificationService _notificationService = notificationService;

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public string? GuestNotes { get; set; }

    public GuestPortalAccess? Access { get; set; }
    public GuestPortalSetting Setting { get; set; } = new();
    public bool CanRequestExpressCheckout { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadStateAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadStateAsync();
        if (Access?.Reservation is null || !CanRequestExpressCheckout)
        {
            ModelState.AddModelError(string.Empty, "Express checkout is not available for this stay.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var request = new ExpressCheckoutRequest
        {
            ReservationId = Access!.ReservationId!.Value,
            GuestId = Access.Reservation!.GuestId,
            RequestedAt = DateTime.Now,
            Status = ExpressCheckoutRequestStatus.Requested,
            GuestNotes = GuestNotes
        };

        _context.ExpressCheckoutRequests.Add(request);
        await _context.SaveChangesAsync();
        await _notificationService.SendExpressCheckoutRequestedAsync(request);
        StatusMessage = "Your express checkout request has been received. The front desk will verify your folio before checkout.";
        return RedirectToPage(new { token = Token });
    }

    private async Task LoadStateAsync()
    {
        Setting = await _guestPortalService.GetSettingsAsync();
        Access = await _guestPortalService.GetAccessAsync(Token);
        CanRequestExpressCheckout = Access?.Reservation?.Status == ReservationStatus.CheckedIn;
    }
}
