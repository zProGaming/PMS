using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;

namespace Vantage.PMS.Pages;

public class PrivacyModel(ApplicationDbContext context) : PageModel
{
    public string ControllerName { get; private set; } = "The hotel operating this PMS";

    public string? ContactEmail { get; private set; }

    public string? PrivacyPolicy { get; private set; }

    public bool HasPublishedNotice => !string.IsNullOrWhiteSpace(PrivacyPolicy);

    public async Task OnGetAsync()
    {
        var bookingNotice = await context.BookingEngineSettings
            .AsNoTracking()
            .Where(setting => setting.IsBookingEngineEnabled && !string.IsNullOrWhiteSpace(setting.PrivacyPolicy))
            .OrderByDescending(setting => setting.UpdatedAt)
            .Select(setting => new { setting.HotelName, setting.ContactEmail, setting.PrivacyPolicy })
            .FirstOrDefaultAsync();

        if (bookingNotice is not null)
        {
            ControllerName = string.IsNullOrWhiteSpace(bookingNotice.HotelName)
                ? ControllerName
                : bookingNotice.HotelName;
            ContactEmail = bookingNotice.ContactEmail;
            PrivacyPolicy = bookingNotice.PrivacyPolicy;
            return;
        }

        PrivacyPolicy = await context.GuestPortalSettings
            .AsNoTracking()
            .Where(setting => setting.IsGuestPortalEnabled && !string.IsNullOrWhiteSpace(setting.PrivacyPolicy))
            .OrderByDescending(setting => setting.UpdatedAt)
            .Select(setting => setting.PrivacyPolicy)
            .FirstOrDefaultAsync();
    }
}
