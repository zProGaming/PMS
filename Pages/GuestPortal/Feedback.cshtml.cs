using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.GuestPortal;

public class FeedbackModel(
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
    public GuestFeedback Feedback { get; set; } = new() { Rating = 5 };

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
        if (Access is null || !Setting.AllowFeedback)
        {
            ModelState.AddModelError(string.Empty, "Feedback is not available.");
        }

        if (Feedback.Rating is < 1 or > 5)
        {
            ModelState.AddModelError("Feedback.Rating", "Rating must be between 1 and 5.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Feedback.ReservationId = Access!.ReservationId;
        Feedback.GuestId = Access.Reservation?.GuestId ?? Access.GuestId;
        Feedback.SubmittedAt = DateTime.Now;
        Feedback.IsResolved = false;

        _context.GuestFeedbacks.Add(Feedback);
        await _context.SaveChangesAsync();
        await _notificationService.SendFeedbackReceivedAsync(Feedback);
        StatusMessage = "Thank you for your feedback.";
        return RedirectToPage(new { token = Token });
    }

    private async Task LoadStateAsync()
    {
        Setting = await _guestPortalService.GetSettingsAsync();
        Access = await _guestPortalService.GetAccessAsync(Token);
    }
}
