using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.GuestPortal;

public class GuestPreCheckIn
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int GuestId { get; set; }

    public Guest? Guest { get; set; }

    public DateTime? ArrivalTime { get; set; }

    public string? IdType { get; set; }

    public string? IdNumber { get; set; }

    public string? Address { get; set; }

    public string? Nationality { get; set; }

    public DateTime? Birthday { get; set; }

    public string? SpecialRequests { get; set; }

    public bool TermsAccepted { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.Now;

    public GuestPreCheckInStatus Status { get; set; } = GuestPreCheckInStatus.Submitted;
}
