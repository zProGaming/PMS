using Vantage.PMS.Models.Core;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Models.FrontOffice;

public class Reservation
{
    public int Id { get; set; }

    public int PropertyId { get; set; }

    public Property? Property { get; set; }

    public int GuestId { get; set; }

    public Guest? Guest { get; set; }

    public int? RoomTypeId { get; set; }

    public RoomType? RoomType { get; set; }

    public int? RoomId { get; set; }

    public Room? Room { get; set; }

    public string ConfirmationNumber { get; set; } = string.Empty;

    public DateTime ArrivalDate { get; set; }

    public DateTime DepartureDate { get; set; }

    public int Adults { get; set; }

    public int Children { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Folio> Folios { get; set; } = new List<Folio>();
}
