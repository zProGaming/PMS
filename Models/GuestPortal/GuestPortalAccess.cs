using Vantage.PMS.Models.Booking;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.GuestPortal;

public class GuestPortalAccess
{
    public int Id { get; set; }

    public int? ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int? BookingRequestId { get; set; }

    public BookingRequest? BookingRequest { get; set; }

    public int? GuestId { get; set; }

    public Guest? Guest { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public string GuestEmail { get; set; } = string.Empty;

    public string? GuestPhone { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastAccessedAt { get; set; }
}
