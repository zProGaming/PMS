namespace Vantage.PMS.Models.GuestPortal;

public class GuestPortalSetting
{
    public int Id { get; set; }

    public string PortalTitle { get; set; } = "Guest Portal";

    public string? WelcomeMessage { get; set; }

    public bool AllowPreCheckIn { get; set; } = true;

    public bool AllowServiceRequests { get; set; } = true;

    public bool AllowFolioView { get; set; } = true;

    public bool AllowExpressCheckoutRequest { get; set; } = true;

    public bool AllowFeedback { get; set; } = true;

    public bool RequireReservationLookupVerification { get; set; } = true;

    public string? TermsAndConditions { get; set; }

    public string? PrivacyPolicy { get; set; }

    public bool IsGuestPortalEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
