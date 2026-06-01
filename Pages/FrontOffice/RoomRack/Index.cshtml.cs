using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Housekeeping;
using Vantage.PMS.Models.Revenue;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.FrontOffice.RoomRack;

public class IndexModel(ApplicationDbContext context, RevenueManagementService revenueManagement) : PageModel
{
    private static readonly ReservationStatus[] CalendarReservationStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Reserved,
        ReservationStatus.CheckedIn,
        ReservationStatus.CheckedOut,
        ReservationStatus.NoShow
    ];

    private static readonly ReservationStatus[] OccupancyReservationStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Reserved,
        ReservationStatus.CheckedIn
    ];

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Days { get; set; } = 14;

    [BindProperty(SupportsGet = true)]
    public int? RoomTypeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public RoomStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty]
    public QuickReservationInput QuickReservation { get; set; } = new();

    public DateTime EffectiveStartDate { get; private set; }

    public DateTime EffectiveEndDateExclusive { get; private set; }

    public DateTime BusinessDate { get; private set; }

    public IList<RoomRackDateColumn> DateColumns { get; private set; } = [];

    public IList<RoomRackTypeGroup> RoomTypeGroups { get; private set; } = [];

    public SelectList RoomTypeOptions { get; private set; } = default!;

    public SelectList StatusOptions { get; private set; } = default!;

    public SelectList ReservationGuestOptions { get; private set; } = default!;

    public SelectList ReservationRatePlanOptions { get; private set; } = default!;

    public IEnumerable<SelectListItem> ReservationStatusOptions { get; private set; } = Enumerable.Empty<SelectListItem>();

    public int TotalRooms { get; private set; }

    public int AvailableRooms { get; private set; }

    public int OccupiedRooms { get; private set; }

    public int DirtyRooms { get; private set; }

    public int OutOfOrderRooms { get; private set; }

    public int OpenHousekeepingTasks { get; private set; }

    public int ArrivalsInRange { get; private set; }

    public int DeparturesInRange { get; private set; }

    public int UnassignedReservationsInRange { get; private set; }

    public decimal AverageOccupancyInRange { get; private set; }

    public string PreviousStartDate => EffectiveStartDate.AddDays(-Days).ToString("yyyy-MM-dd");

    public string NextStartDate => EffectiveStartDate.AddDays(Days).ToString("yyyy-MM-dd");

    public async Task OnGetAsync()
    {
        BusinessDate = await context.BusinessDateSettings
            .AsNoTracking()
            .OrderBy(setting => setting.Id)
            .Select(setting => (DateTime?)setting.CurrentBusinessDate)
            .FirstOrDefaultAsync() ?? DateTime.Today;

        NormalizeDateRange();

        var allActiveRooms = await context.Rooms
            .Include(room => room.Property)
            .Include(room => room.RoomType)
            .AsNoTracking()
            .Where(room => room.IsActive)
            .OrderBy(room => room.RoomType!.Name)
            .ThenBy(room => room.RoomNumber)
            .ToListAsync();

        var checkedInRoomIds = await context.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.Status == ReservationStatus.CheckedIn && reservation.RoomId != null)
            .Select(reservation => reservation.RoomId!.Value)
            .Distinct()
            .ToListAsync();

        var checkedInRoomSet = checkedInRoomIds.ToHashSet();

        TotalRooms = allActiveRooms.Count;
        AvailableRooms = allActiveRooms.Count(room => room.Status is RoomStatus.Available or RoomStatus.Clean or RoomStatus.Inspected);
        OccupiedRooms = allActiveRooms.Count(room => room.Status == RoomStatus.Occupied || checkedInRoomSet.Contains(room.Id));
        DirtyRooms = allActiveRooms.Count(room => room.Status == RoomStatus.Dirty);
        OutOfOrderRooms = allActiveRooms.Count(room => room.Status is RoomStatus.OutOfOrder or RoomStatus.Maintenance);

        await LoadSelectListsAsync();
        await LoadQuickReservationOptionsAsync();

        var visibleRooms = allActiveRooms
            .Where(room => RoomTypeId is null || room.RoomTypeId == RoomTypeId)
            .Where(room => Status is null || room.Status == Status)
            .ToList();

        var visibleRoomIds = visibleRooms.Select(room => room.Id).ToList();
        var visibleRoomIdSet = visibleRoomIds.ToHashSet();

        UnassignedReservationsInRange = await context.Reservations
            .AsNoTracking()
            .CountAsync(reservation =>
                reservation.RoomId == null &&
                CalendarReservationStatuses.Contains(reservation.Status) &&
                reservation.ArrivalDate < EffectiveEndDateExclusive &&
                reservation.DepartureDate > EffectiveStartDate);

        var reservations = await context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .Include(reservation => reservation.RoomType)
            .AsNoTracking()
            .Where(reservation =>
                reservation.RoomId != null &&
                visibleRoomIds.Contains(reservation.RoomId.Value) &&
                CalendarReservationStatuses.Contains(reservation.Status) &&
                reservation.ArrivalDate < EffectiveEndDateExclusive &&
                reservation.DepartureDate > EffectiveStartDate)
            .OrderBy(reservation => reservation.ArrivalDate)
            .ThenBy(reservation => reservation.DepartureDate)
            .ToListAsync();

        var openTasks = await context.HousekeepingTasks
            .AsNoTracking()
            .Where(task =>
                visibleRoomIds.Contains(task.RoomId) &&
                task.TaskStatus != HousekeepingTaskStatus.Completed &&
                task.TaskStatus != HousekeepingTaskStatus.Cancelled)
            .ToListAsync();

        OpenHousekeepingTasks = openTasks.Count;

        var roomTypeIds = visibleRooms.Select(room => room.RoomTypeId).Distinct().ToList();
        var roomTypeRates = await context.RoomTypeRates
            .Include(rate => rate.RatePlan)
            .AsNoTracking()
            .Where(rate =>
                roomTypeIds.Contains(rate.RoomTypeId) &&
                rate.IsActive &&
                rate.EffectiveFrom < EffectiveEndDateExclusive &&
                rate.EffectiveTo >= EffectiveStartDate)
            .OrderByDescending(rate => rate.EffectiveFrom)
            .ThenBy(rate => rate.RatePlanId)
            .ToListAsync();

        var inventoryControls = await context.RoomInventoryControls
            .AsNoTracking()
            .Where(control =>
                roomTypeIds.Contains(control.RoomTypeId) &&
                control.InventoryDate >= EffectiveStartDate &&
                control.InventoryDate < EffectiveEndDateExclusive)
            .ToListAsync();

        ArrivalsInRange = reservations.Count(reservation => reservation.ArrivalDate >= EffectiveStartDate && reservation.ArrivalDate < EffectiveEndDateExclusive);
        DeparturesInRange = reservations.Count(reservation => reservation.DepartureDate > EffectiveStartDate && reservation.DepartureDate <= EffectiveEndDateExclusive);

        DateColumns = Enumerable.Range(0, Days)
            .Select(offset => BuildDateColumn(
                EffectiveStartDate.AddDays(offset),
                visibleRooms,
                reservations,
                inventoryControls))
            .ToList();

        AverageOccupancyInRange = DateColumns.Count == 0 ? 0 : DateColumns.Average(day => day.OccupancyPercentage);

        var reservationsByRoom = reservations
            .GroupBy(reservation => reservation.RoomId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var tasksByRoom = openTasks
            .GroupBy(task => task.RoomId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var roomRows = visibleRooms
            .Where(room => visibleRoomIdSet.Contains(room.Id))
            .Select(room => BuildRoomRow(room, reservationsByRoom, tasksByRoom, inventoryControls))
            .ToList();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var searchTerms = Search
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            roomRows = roomRows
                .Where(room =>
                {
                    var haystack = $"{room.RoomNumber} room {room.RoomTypeName} {room.PropertyName} {string.Join(' ', room.ReservationBars.Select(bar => $"{bar.GuestName} {bar.ConfirmationNumber}"))}";
                    return searchTerms.All(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase));
                })
                .ToList();
        }

        RoomTypeGroups = roomRows
            .GroupBy(room => new { room.RoomTypeId, room.RoomTypeName })
            .Select(group => new RoomRackTypeGroup
            {
                RoomTypeId = group.Key.RoomTypeId,
                RoomTypeName = group.Key.RoomTypeName,
                Rooms = group.OrderBy(room => room.RoomNumber).ToList(),
                DateSummaries = DateColumns
                    .Select(date => BuildTypeDateSummary(date.Date, group.ToList(), reservations, roomTypeRates, inventoryControls))
                    .ToList()
            })
            .OrderBy(group => group.RoomTypeName)
            .ToList();
    }

    public async Task<IActionResult> OnPostQuickReservationAsync()
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        void AddError(string field, string message)
        {
            if (!errors.TryGetValue(field, out var messages))
            {
                messages = [];
                errors[field] = messages;
            }

            messages.Add(message);
        }

        if (QuickReservation.GuestId <= 0)
        {
            AddError(nameof(QuickReservation.GuestId), "Select a guest profile.");
        }

        if (QuickReservation.RoomId <= 0)
        {
            AddError(nameof(QuickReservation.RoomId), "Select a room from the calendar.");
        }

        if (QuickReservation.DepartureDate.Date <= QuickReservation.ArrivalDate.Date)
        {
            AddError(nameof(QuickReservation.DepartureDate), "Check-out date must be after check-in date.");
        }

        if (QuickReservation.Adults < 0 || QuickReservation.Children < 0)
        {
            AddError(nameof(QuickReservation.Adults), "Guest counts cannot be negative.");
        }

        if (QuickReservation.RateAmount < 0)
        {
            AddError(nameof(QuickReservation.RateAmount), "Rate amount cannot be negative.");
        }

        var guestExists = QuickReservation.GuestId > 0 &&
            await context.Guests.AsNoTracking().AnyAsync(guest => guest.Id == QuickReservation.GuestId);

        if (QuickReservation.GuestId > 0 && !guestExists)
        {
            AddError(nameof(QuickReservation.GuestId), "The selected guest was not found.");
        }

        var room = QuickReservation.RoomId > 0
            ? await context.Rooms
                .Include(room => room.RoomType)
                .AsNoTracking()
                .FirstOrDefaultAsync(room => room.Id == QuickReservation.RoomId)
            : null;

        if (QuickReservation.RoomId > 0 && room is null)
        {
            AddError(nameof(QuickReservation.RoomId), "The selected room was not found.");
        }
        else if (room is not null && (!room.IsActive || !IsAssignableRoomStatus(room.Status)))
        {
            AddError(nameof(QuickReservation.RoomId), $"Room {room.RoomNumber} is {FormatEnum(room.Status.ToString())} and cannot be assigned.");
        }

        if (room is not null && QuickReservation.DepartureDate.Date > QuickReservation.ArrivalDate.Date)
        {
            var hasConflict = await context.Reservations
                .AsNoTracking()
                .AnyAsync(reservation =>
                    reservation.RoomId == room.Id &&
                    reservation.Status != ReservationStatus.Cancelled &&
                    reservation.Status != ReservationStatus.CheckedOut &&
                    reservation.Status != ReservationStatus.NoShow &&
                    reservation.ArrivalDate.Date < QuickReservation.DepartureDate.Date &&
                    reservation.DepartureDate.Date > QuickReservation.ArrivalDate.Date);

            if (hasConflict)
            {
                AddError(nameof(QuickReservation.RoomId), "The selected room already has an active reservation during these stay dates.");
            }
        }

        if (room is not null)
        {
            var suggestedRate = await revenueManagement.GetSuggestedRateAsync(
                QuickReservation.RatePlanId,
                room.RoomTypeId,
                QuickReservation.ArrivalDate.Date,
                QuickReservation.DepartureDate.Date);

            if (QuickReservation.RateAmount <= 0 && suggestedRate > 0)
            {
                QuickReservation.RateAmount = suggestedRate;
            }

            var controlErrors = await revenueManagement.ValidateReservationControlsAsync(
                null,
                QuickReservation.RatePlanId,
                room.RoomTypeId,
                QuickReservation.ArrivalDate.Date,
                QuickReservation.DepartureDate.Date);

            foreach (var error in controlErrors)
            {
                AddError(string.Empty, error);
            }
        }

        if (errors.Count > 0)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Reservation could not be created. Review the highlighted items.",
                errors
            });
        }

        var reservation = new Reservation
        {
            PropertyId = room!.PropertyId,
            GuestId = QuickReservation.GuestId,
            RoomTypeId = room.RoomTypeId,
            RoomId = room.Id,
            RatePlanId = QuickReservation.RatePlanId,
            ConfirmationNumber = CreateConfirmationNumber(),
            ArrivalDate = QuickReservation.ArrivalDate.Date,
            DepartureDate = QuickReservation.DepartureDate.Date,
            RateAmount = QuickReservation.RateAmount,
            Adults = QuickReservation.Adults,
            Children = QuickReservation.Children,
            Status = QuickReservation.Status,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        return new JsonResult(new
        {
            success = true,
            reservationId = reservation.Id,
            confirmationNumber = reservation.ConfirmationNumber,
            reservationUrl = Url.Page("/FrontOffice/Reservations/Details", new { id = reservation.Id }),
            message = $"Reservation {reservation.ConfirmationNumber} created for Room {room.RoomNumber}."
        });
    }

    public string BuildRouteFor(DateTime startDate, int? days = null)
    {
        var query = new Dictionary<string, string?>
        {
            ["StartDate"] = startDate.ToString("yyyy-MM-dd"),
            ["Days"] = (days ?? Days).ToString(),
            ["RoomTypeId"] = RoomTypeId?.ToString(),
            ["Status"] = Status?.ToString(),
            ["Search"] = Search
        };

        var queryString = string.Join("&", query
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value!)}"));

        return string.IsNullOrWhiteSpace(queryString) ? "/FrontOffice/RoomRack" : $"/FrontOffice/RoomRack?{queryString}";
    }

    private void NormalizeDateRange()
    {
        EffectiveStartDate = (StartDate ?? BusinessDate).Date;
        StartDate = EffectiveStartDate;
        Days = Days switch
        {
            < 1 => 14,
            > 31 => 31,
            _ => Days
        };
        EffectiveEndDateExclusive = EffectiveStartDate.AddDays(Days);
    }

    private async Task LoadSelectListsAsync()
    {
        var roomTypes = await context.RoomTypes
            .AsNoTracking()
            .Where(roomType => roomType.IsActive)
            .OrderBy(roomType => roomType.Name)
            .Select(roomType => new { roomType.Id, roomType.Name })
            .ToListAsync();

        RoomTypeOptions = new SelectList(roomTypes, "Id", "Name", RoomTypeId);
        StatusOptions = new SelectList(
            Enum.GetValues<RoomStatus>().Select(status => new
            {
                Id = status.ToString(),
                Name = FormatEnum(status.ToString())
            }),
            "Id",
            "Name",
            Status?.ToString());
    }

    private async Task LoadQuickReservationOptionsAsync()
    {
        var guests = await context.Guests
            .AsNoTracking()
            .OrderBy(guest => guest.LastName)
            .ThenBy(guest => guest.FirstName)
            .Select(guest => new { guest.Id, Name = guest.LastName + ", " + guest.FirstName })
            .ToListAsync();

        var ratePlans = await context.RatePlans
            .AsNoTracking()
            .Where(ratePlan => ratePlan.IsActive)
            .OrderBy(ratePlan => ratePlan.Code)
            .Select(ratePlan => new { ratePlan.Id, Name = ratePlan.Code + " - " + ratePlan.Name })
            .ToListAsync();

        ReservationGuestOptions = new SelectList(guests, "Id", "Name");
        ReservationRatePlanOptions = new SelectList(ratePlans, "Id", "Name");
        ReservationStatusOptions = Enum.GetValues<ReservationStatus>()
            .Where(status => status is ReservationStatus.Pending or ReservationStatus.Reserved)
            .Select(status => new SelectListItem
            {
                Value = status.ToString(),
                Text = FormatEnum(status.ToString()),
                Selected = status == ReservationStatus.Reserved
            })
            .ToList();
    }

    private RoomRackDateColumn BuildDateColumn(
        DateTime date,
        IList<Room> visibleRooms,
        IList<Reservation> reservations,
        IList<RoomInventoryControl> inventoryControls)
    {
        var activeReservationRoomIds = reservations
            .Where(reservation => IsOccupyingDate(reservation, date))
            .Select(reservation => reservation.RoomId!.Value)
            .Distinct()
            .ToHashSet();

        if (date.Date == BusinessDate.Date)
        {
            foreach (var roomId in visibleRooms.Where(room => room.Status == RoomStatus.Occupied).Select(room => room.Id))
            {
                activeReservationRoomIds.Add(roomId);
            }
        }

        var unavailableRooms = visibleRooms.Count(room => room.Status is RoomStatus.OutOfOrder or RoomStatus.Maintenance);
        var availableRooms = Math.Max(0, visibleRooms.Count - activeReservationRoomIds.Count - unavailableRooms);
        var stopSellCount = inventoryControls.Count(control => control.InventoryDate.Date == date.Date && control.StopSell);

        return new RoomRackDateColumn
        {
            Date = date,
            AvailableRooms = availableRooms,
            OccupiedRooms = activeReservationRoomIds.Count,
            OccupancyPercentage = visibleRooms.Count == 0 ? 0 : Math.Round(activeReservationRoomIds.Count * 100m / visibleRooms.Count, 1),
            StopSellRoomTypes = stopSellCount,
            IsToday = date.Date == BusinessDate.Date,
            IsWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
        };
    }

    private RoomRackCalendarRoom BuildRoomRow(
        Room room,
        IReadOnlyDictionary<int, List<Reservation>> reservationsByRoom,
        IReadOnlyDictionary<int, List<HousekeepingTask>> tasksByRoom,
        IList<RoomInventoryControl> inventoryControls)
    {
        reservationsByRoom.TryGetValue(room.Id, out var roomReservations);
        tasksByRoom.TryGetValue(room.Id, out var roomTasks);

        roomReservations ??= [];
        roomTasks ??= [];

        return new RoomRackCalendarRoom
        {
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            Floor = string.IsNullOrWhiteSpace(room.Floor) ? "Unassigned" : room.Floor,
            PropertyName = room.Property?.Name ?? "Property not set",
            RoomTypeId = room.RoomTypeId,
            RoomTypeName = room.RoomType?.Name ?? "Room type not set",
            RoomTypeCode = room.RoomType?.Code ?? string.Empty,
            BaseRate = room.RoomType?.BaseRate ?? 0,
            Status = room.Status,
            StatusNotes = room.StatusNotes,
            OpenTaskCount = roomTasks.Count,
            UrgentTaskCount = roomTasks.Count(task => task.Priority == HousekeepingTaskPriority.Urgent),
            ReservationBars = roomReservations.Select(BuildReservationBar).ToList(),
            Cells = DateColumns.Select(date => BuildRoomCell(room, date.Date, roomReservations, roomTasks, inventoryControls)).ToList()
        };
    }

    private RoomRackCalendarCell BuildRoomCell(
        Room room,
        DateTime date,
        IList<Reservation> roomReservations,
        IList<HousekeepingTask> roomTasks,
        IList<RoomInventoryControl> inventoryControls)
    {
        var activeReservation = roomReservations.FirstOrDefault(reservation => IsOccupyingDate(reservation, date));
        var arrival = roomReservations.Any(reservation => reservation.ArrivalDate.Date == date.Date);
        var departure = roomReservations.Any(reservation => reservation.DepartureDate.Date == date.Date);
        var stopSell = inventoryControls.Any(control => control.RoomTypeId == room.RoomTypeId && control.InventoryDate.Date == date.Date && control.StopSell);

        return new RoomRackCalendarCell
        {
            Date = date,
            RoomStatus = room.Status,
            ReservationId = activeReservation?.Id,
            HasArrival = arrival,
            HasDeparture = departure,
            HasOpenTask = roomTasks.Count > 0,
            HasUrgentTask = roomTasks.Any(task => task.Priority == HousekeepingTaskPriority.Urgent),
            IsStopSell = stopSell,
            IsToday = date.Date == BusinessDate.Date,
            IsCurrentOccupied = date.Date == BusinessDate.Date && room.Status == RoomStatus.Occupied,
            IsCurrentDirty = date.Date == BusinessDate.Date && room.Status == RoomStatus.Dirty,
            IsCurrentReady = date.Date == BusinessDate.Date && room.Status is RoomStatus.Available or RoomStatus.Clean or RoomStatus.Inspected,
            IsUnavailable = room.Status is RoomStatus.OutOfOrder or RoomStatus.Maintenance
        };
    }

    private RoomRackReservationBar BuildReservationBar(Reservation reservation)
    {
        var start = reservation.ArrivalDate.Date < EffectiveStartDate ? EffectiveStartDate : reservation.ArrivalDate.Date;
        var end = reservation.DepartureDate.Date > EffectiveEndDateExclusive ? EffectiveEndDateExclusive : reservation.DepartureDate.Date;
        var span = Math.Max(1, (end - start).Days);
        var startOffset = Math.Max(0, (start - EffectiveStartDate).Days) + 1;
        var guestName = reservation.Guest is null
            ? "Guest pending"
            : $"{reservation.Guest.FirstName} {reservation.Guest.LastName}".Trim();

        return new RoomRackReservationBar
        {
            ReservationId = reservation.Id,
            ConfirmationNumber = reservation.ConfirmationNumber,
            GuestName = string.IsNullOrWhiteSpace(guestName) ? "Guest pending" : guestName,
            Status = reservation.Status,
            StartOffset = startOffset,
            Span = span,
            ArrivalDate = reservation.ArrivalDate.Date,
            DepartureDate = reservation.DepartureDate.Date,
            RateAmount = reservation.RateAmount,
            IsContinuedBefore = reservation.ArrivalDate.Date < EffectiveStartDate,
            IsContinuedAfter = reservation.DepartureDate.Date > EffectiveEndDateExclusive
        };
    }

    private RoomRackTypeDateSummary BuildTypeDateSummary(
        DateTime date,
        IList<RoomRackCalendarRoom> rooms,
        IList<Reservation> reservations,
        IList<RoomTypeRate> roomTypeRates,
        IList<RoomInventoryControl> inventoryControls)
    {
        var roomIds = rooms.Select(room => room.RoomId).ToHashSet();
        var roomTypeId = rooms.FirstOrDefault()?.RoomTypeId ?? 0;
        var occupiedRooms = reservations
            .Where(reservation => reservation.RoomId != null && roomIds.Contains(reservation.RoomId.Value) && IsOccupyingDate(reservation, date))
            .Select(reservation => reservation.RoomId!.Value)
            .Distinct()
            .ToHashSet();
        if (date.Date == BusinessDate.Date)
        {
            foreach (var roomId in rooms.Where(room => room.Status == RoomStatus.Occupied).Select(room => room.RoomId))
            {
                occupiedRooms.Add(roomId);
            }
        }
        var unavailableRooms = rooms.Count(room => room.Status is RoomStatus.OutOfOrder or RoomStatus.Maintenance);
        var availableRooms = Math.Max(0, rooms.Count - occupiedRooms.Count - unavailableRooms);
        var stopSell = inventoryControls.Any(control => control.RoomTypeId == roomTypeId && control.InventoryDate.Date == date.Date && control.StopSell);
        var rate = roomTypeRates
            .Where(rate => rate.RoomTypeId == roomTypeId && rate.EffectiveFrom.Date <= date.Date && rate.EffectiveTo.Date >= date.Date)
            .OrderByDescending(rate => rate.EffectiveFrom)
            .Select(rate => (decimal?)rate.BaseRate)
            .FirstOrDefault() ?? rooms.FirstOrDefault()?.BaseRate ?? 0;

        return new RoomRackTypeDateSummary
        {
            Date = date,
            AvailableRooms = availableRooms,
            OccupiedRooms = occupiedRooms.Count,
            OccupancyPercentage = rooms.Count == 0 ? 0 : Math.Round(occupiedRooms.Count * 100m / rooms.Count, 1),
            DisplayRate = rate,
            IsStopSell = stopSell,
            IsToday = date.Date == BusinessDate.Date
        };
    }

    private static bool IsOccupyingDate(Reservation reservation, DateTime date)
    {
        return OccupancyReservationStatuses.Contains(reservation.Status) &&
            reservation.ArrivalDate.Date <= date.Date &&
            reservation.DepartureDate.Date > date.Date;
    }

    public static string FormatEnum(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = new List<char> { value[0] };
        foreach (var character in value.Skip(1))
        {
            if (char.IsUpper(character))
            {
                chars.Add(' ');
            }
            chars.Add(character);
        }

        return new string(chars.ToArray());
    }

    public static string BuildCellFocusText(RoomRackCalendarRoom room, RoomRackCalendarCell cell)
    {
        var signals = new List<string> { FormatEnum(room.Status.ToString()) };

        if (cell.ReservationId.HasValue)
        {
            signals.Add("active stay");
        }

        if (cell.HasArrival)
        {
            signals.Add("arrival");
        }

        if (cell.HasDeparture)
        {
            signals.Add("departure");
        }

        if (cell.HasUrgentTask)
        {
            signals.Add("urgent housekeeping");
        }
        else if (cell.HasOpenTask)
        {
            signals.Add("housekeeping task");
        }

        if (cell.IsStopSell)
        {
            signals.Add("stop sell");
        }

        if (cell.IsUnavailable)
        {
            signals.Add("not sellable");
        }

        return $"Room {room.RoomNumber} on {cell.Date:MMM d}: {string.Join(", ", signals)}.";
    }

    private static string CreateConfirmationNumber()
    {
        return $"RES-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private static bool IsAssignableRoomStatus(RoomStatus status)
    {
        return status is RoomStatus.Available or RoomStatus.Clean or RoomStatus.Inspected;
    }
}

public class QuickReservationInput
{
    [Required]
    public int GuestId { get; set; }

    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateTime ArrivalDate { get; set; }

    [Required]
    public DateTime DepartureDate { get; set; }

    public int? RatePlanId { get; set; }

    [Range(0, 999999999)]
    public decimal RateAmount { get; set; }

    [Range(0, 20)]
    public int Adults { get; set; } = 1;

    [Range(0, 20)]
    public int Children { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;
}

public class RoomRackDateColumn
{
    public DateTime Date { get; set; }

    public int AvailableRooms { get; set; }

    public int OccupiedRooms { get; set; }

    public decimal OccupancyPercentage { get; set; }

    public int StopSellRoomTypes { get; set; }

    public bool IsToday { get; set; }

    public bool IsWeekend { get; set; }
}

public class RoomRackTypeGroup
{
    public int RoomTypeId { get; set; }

    public string RoomTypeName { get; set; } = string.Empty;

    public IList<RoomRackTypeDateSummary> DateSummaries { get; set; } = [];

    public IList<RoomRackCalendarRoom> Rooms { get; set; } = [];
}

public class RoomRackTypeDateSummary
{
    public DateTime Date { get; set; }

    public int AvailableRooms { get; set; }

    public int OccupiedRooms { get; set; }

    public decimal OccupancyPercentage { get; set; }

    public decimal DisplayRate { get; set; }

    public bool IsStopSell { get; set; }

    public bool IsToday { get; set; }
}

public class RoomRackCalendarRoom
{
    public int RoomId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public string Floor { get; set; } = string.Empty;

    public string PropertyName { get; set; } = string.Empty;

    public int RoomTypeId { get; set; }

    public string RoomTypeName { get; set; } = string.Empty;

    public string RoomTypeCode { get; set; } = string.Empty;

    public decimal BaseRate { get; set; }

    public RoomStatus Status { get; set; }

    public string? StatusNotes { get; set; }

    public int OpenTaskCount { get; set; }

    public int UrgentTaskCount { get; set; }

    public IList<RoomRackCalendarCell> Cells { get; set; } = [];

    public IList<RoomRackReservationBar> ReservationBars { get; set; } = [];
}

public class RoomRackCalendarCell
{
    public DateTime Date { get; set; }

    public RoomStatus RoomStatus { get; set; }

    public int? ReservationId { get; set; }

    public bool HasArrival { get; set; }

    public bool HasDeparture { get; set; }

    public bool HasOpenTask { get; set; }

    public bool HasUrgentTask { get; set; }

    public bool IsStopSell { get; set; }

    public bool IsToday { get; set; }

    public bool IsCurrentOccupied { get; set; }

    public bool IsCurrentDirty { get; set; }

    public bool IsCurrentReady { get; set; }

    public bool IsUnavailable { get; set; }
}

public class RoomRackReservationBar
{
    public int ReservationId { get; set; }

    public string ConfirmationNumber { get; set; } = string.Empty;

    public string GuestName { get; set; } = string.Empty;

    public ReservationStatus Status { get; set; }

    public int StartOffset { get; set; }

    public int Span { get; set; }

    public DateTime ArrivalDate { get; set; }

    public DateTime DepartureDate { get; set; }

    public decimal RateAmount { get; set; }

    public bool IsContinuedBefore { get; set; }

    public bool IsContinuedAfter { get; set; }
}
