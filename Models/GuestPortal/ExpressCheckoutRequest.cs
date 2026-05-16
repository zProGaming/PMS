using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.GuestPortal;

public class ExpressCheckoutRequest
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int GuestId { get; set; }

    public Guest? Guest { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.Now;

    public ExpressCheckoutRequestStatus Status { get; set; } = ExpressCheckoutRequestStatus.Requested;

    public string? GuestNotes { get; set; }

    public string? StaffNotes { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? ProcessedBy { get; set; }
}
