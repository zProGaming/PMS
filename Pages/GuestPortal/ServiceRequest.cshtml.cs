using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.GuestPortal;

public class ServiceRequestModel(
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
    public GuestServiceRequest ServiceRequest { get; set; } = new();

    public GuestPortalAccess? Access { get; set; }
    public GuestPortalSetting Setting { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadStateAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadStateAsync();
        if (Access is null || !Setting.AllowServiceRequests)
        {
            ModelState.AddModelError(string.Empty, "Service requests are not available.");
        }

        if (string.IsNullOrWhiteSpace(ServiceRequest.Description))
        {
            ModelState.AddModelError("ServiceRequest.Description", "Description is required.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        ServiceRequest.ReservationId = Access!.ReservationId;
        ServiceRequest.GuestId = Access.Reservation?.GuestId ?? Access.GuestId;
        ServiceRequest.RoomId = Access.Reservation?.RoomId;
        ServiceRequest.Status = GuestServiceRequestStatus.New;
        ServiceRequest.CreatedAt = DateTime.Now;

        _context.GuestServiceRequests.Add(ServiceRequest);
        await _context.SaveChangesAsync();
        await _notificationService.SendServiceRequestReceivedAsync(ServiceRequest);
        StatusMessage = "Your service request has been submitted.";
        return RedirectToPage(new { token = Token });
    }

    private async Task LoadStateAsync()
    {
        Setting = await _guestPortalService.GetSettingsAsync();
        Access = await _guestPortalService.GetAccessAsync(Token);
    }
}
