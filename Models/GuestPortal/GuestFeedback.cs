using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.GuestPortal;

public class GuestFeedback
{
    public int Id { get; set; }

    public int? ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int? GuestId { get; set; }

    public Guest? Guest { get; set; }

    public int Rating { get; set; }

    public string? Comments { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.Now;

    public bool IsResolved { get; set; }

    public string? ResolutionNotes { get; set; }
}
