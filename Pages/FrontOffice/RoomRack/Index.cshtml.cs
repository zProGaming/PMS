using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.RoomRack;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<RoomRackFloorGroup> FloorGroups { get; private set; } = [];

    public int TotalRooms { get; private set; }

    public int AvailableRooms { get; private set; }

    public int OccupiedRooms { get; private set; }

    public int DirtyRooms { get; private set; }

    public int OutOfOrderRooms { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;

        var activeReservations = await context.Reservations
            .Include(reservation => reservation.Guest)
            .AsNoTracking()
            .Where(reservation =>
                reservation.RoomId != null &&
                (reservation.Status == ReservationStatus.CheckedIn ||
                    (reservation.Status == ReservationStatus.Reserved &&
                     reservation.ArrivalDate.Date <= today &&
                     reservation.DepartureDate.Date > today)))
            .OrderByDescending(reservation => reservation.Status == ReservationStatus.CheckedIn)
            .ThenBy(reservation => reservation.ArrivalDate)
            .ToListAsync();

        var reservationByRoom = activeReservations
            .GroupBy(reservation => reservation.RoomId!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        var rooms = await context.Rooms
            .Include(room => room.Property)
            .Include(room => room.RoomType)
            .AsNoTracking()
            .Where(room => room.IsActive)
            .OrderBy(room => room.Floor)
            .ThenBy(room => room.RoomNumber)
            .ToListAsync();

        TotalRooms = rooms.Count;
        AvailableRooms = rooms.Count(room => room.Status is RoomStatus.Available or RoomStatus.Clean or RoomStatus.Inspected);
        OccupiedRooms = rooms.Count(room => room.Status == RoomStatus.Occupied);
        DirtyRooms = rooms.Count(room => room.Status == RoomStatus.Dirty);
        OutOfOrderRooms = rooms.Count(room => room.Status is RoomStatus.OutOfOrder or RoomStatus.Maintenance);

        FloorGroups = rooms
            .Select(room =>
            {
                reservationByRoom.TryGetValue(room.Id, out var reservation);
                return new RoomRackItem
                {
                    RoomId = room.Id,
                    RoomNumber = room.RoomNumber,
                    Floor = string.IsNullOrWhiteSpace(room.Floor) ? "Unassigned" : room.Floor,
                    PropertyName = room.Property?.Name ?? "Property not set",
                    RoomTypeName = room.RoomType?.Name ?? "Room type not set",
                    Status = room.Status,
                    StatusNotes = room.StatusNotes,
                    ReservationId = reservation?.Id,
                    ConfirmationNumber = reservation?.ConfirmationNumber,
                    GuestName = reservation?.Guest is null
                        ? null
                        : $"{reservation.Guest.FirstName} {reservation.Guest.LastName}".Trim(),
                    ArrivalDate = reservation?.ArrivalDate,
                    DepartureDate = reservation?.DepartureDate,
                    ReservationStatus = reservation?.Status
                };
            })
            .GroupBy(room => room.Floor)
            .Select(group => new RoomRackFloorGroup
            {
                Floor = group.Key,
                Rooms = group.OrderBy(room => room.RoomNumber).ToList()
            })
            .OrderBy(group => group.Floor)
            .ToList();
    }
}

public class RoomRackFloorGroup
{
    public string Floor { get; set; } = string.Empty;

    public IList<RoomRackItem> Rooms { get; set; } = [];
}

public class RoomRackItem
{
    public int RoomId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public string Floor { get; set; } = string.Empty;

    public string PropertyName { get; set; } = string.Empty;

    public string RoomTypeName { get; set; } = string.Empty;

    public RoomStatus Status { get; set; }

    public string? StatusNotes { get; set; }

    public int? ReservationId { get; set; }

    public string? ConfirmationNumber { get; set; }

    public string? GuestName { get; set; }

    public DateTime? ArrivalDate { get; set; }

    public DateTime? DepartureDate { get; set; }

    public ReservationStatus? ReservationStatus { get; set; }
}
