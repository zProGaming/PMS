using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.GuestPortal;

public class PreCheckInModel(
    GuestPortalService guestPortalService,
    GuestPortalNotificationService notificationService,
    Vantage.PMS.Data.ApplicationDbContext context) : PageModel
{
    private readonly GuestPortalService _guestPortalService = guestPortalService;
    private readonly GuestPortalNotificationService _notificationService = notificationService;
    private readonly Vantage.PMS.Data.ApplicationDbContext _context = context;

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public GuestPreCheckIn PreCheckIn { get; set; } = new() { TermsAccepted = false };

    public GuestPortalAccess? Access { get; set; }
    public GuestPortalSetting Setting { get; set; } = new();
    public bool CanSubmitPreCheckIn { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadStateAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadStateAsync();
        if (Access?.Reservation is null || !CanSubmitPreCheckIn)
        {
            ModelState.AddModelError(string.Empty, "Pre-check-in is not available for this stay.");
        }

        if (!PreCheckIn.TermsAccepted)
        {
            ModelState.AddModelError(string.Empty, "Terms acceptance is required.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        PreCheckIn.ReservationId = Access!.ReservationId!.Value;
        PreCheckIn.GuestId = Access.Reservation!.GuestId;
        PreCheckIn.Status = GuestPreCheckInStatus.Submitted;
        PreCheckIn.SubmittedAt = DateTime.Now;

        _context.GuestPreCheckIns.Add(PreCheckIn);
        await _context.SaveChangesAsync();
        await _notificationService.SendPreCheckInReceivedAsync(PreCheckIn);
        StatusMessage = "Your pre-check-in details have been submitted.";
        return RedirectToPage(new { token = Token });
    }

    private async Task LoadStateAsync()
    {
        Setting = await _guestPortalService.GetSettingsAsync();
        Access = await _guestPortalService.GetAccessAsync(Token);
        CanSubmitPreCheckIn = Access?.Reservation is not null &&
            (Access.Reservation.Status == ReservationStatus.Reserved || Access.Reservation.Status == ReservationStatus.Pending) &&
            Access.Reservation.ArrivalDate.Date >= DateTime.Today;
    }
}
