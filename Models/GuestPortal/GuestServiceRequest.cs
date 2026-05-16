using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.GuestPortal;

public class GuestServiceRequest
{
    public int Id { get; set; }

    public int? ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int? GuestId { get; set; }

    public Guest? Guest { get; set; }

    public int? RoomId { get; set; }

    public Room? Room { get; set; }

    public GuestServiceRequestType RequestType { get; set; } = GuestServiceRequestType.Other;

    public GuestServiceRequestPriority Priority { get; set; } = GuestServiceRequestPriority.Normal;

    public string Description { get; set; } = string.Empty;

    public GuestServiceRequestStatus Status { get; set; } = GuestServiceRequestStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? CompletedAt { get; set; }

    public string? AssignedTo { get; set; }

    public string? Notes { get; set; }
}
