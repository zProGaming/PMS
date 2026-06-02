using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Groups;

public class DetailsModel(ApplicationDbContext context, GroupManagementService groupManagementService, AuditLogService auditLogService) : PageModel
{
    public GroupBooking GroupBooking { get; private set; } = default!;

    public GroupCollectionSummary CollectionSummary { get; private set; } = new();

    public SelectList RoomTypeOptions { get; private set; } = null!;
    public SelectList RatePlanOptions { get; private set; } = null!;
    public SelectList ReservationOptions { get; private set; } = null!;
    public SelectList PseudoRoomOptions { get; private set; } = null!;
    public SelectList FolioOptions { get; private set; } = null!;
    public SelectList DepositOptions { get; private set; } = null!;
    public SelectList MemberReservationOptions { get; private set; } = null!;
    public SelectList BlockOptions { get; private set; } = null!;
    public SelectList GuestOptions { get; private set; } = null!;
    public SelectList RoomOptions { get; private set; } = null!;

    [BindProperty]
    public GroupBooking EditInput { get; set; } = new();

    [BindProperty]
    public GroupRoomBlock BlockInput { get; set; } = new();

    [BindProperty]
    public GroupMemberReservation MemberInput { get; set; } = new();

    [BindProperty]
    public GroupFolio GroupFolioInput { get; set; } = new();

    [BindProperty]
    public GroupDeposit DepositInput { get; set; } = new();

    [BindProperty]
    public GroupPaymentAllocation AllocationInput { get; set; } = new();

    [BindProperty]
    public GroupPickupInput PickupInput { get; set; } = new();

    public bool CanUseFinanceActions => User.IsInRole(PmsRoles.SystemAdmin) ||
        User.IsInRole(PmsRoles.GeneralManager) ||
        User.IsInRole(PmsRoles.FinanceManager);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadOrNotFoundAsync(id);
    }

    public async Task<IActionResult> OnGetUpdateNativeAsync(int id)
    {
        return await NativePartialOrNotFoundAsync(id, "_UpdateNative");
    }

    public async Task<IActionResult> OnGetAddBlockNativeAsync(int id)
    {
        return await NativePartialOrNotFoundAsync(id, "_AddBlockNative");
    }

    public async Task<IActionResult> OnGetPickupNativeAsync(int id)
    {
        return await NativePartialOrNotFoundAsync(id, "_PickupNative");
    }

    public async Task<IActionResult> OnGetLinkReservationNativeAsync(int id)
    {
        return await NativePartialOrNotFoundAsync(id, "_LinkReservationNative");
    }

    public async Task<IActionResult> OnGetCreateFolioNativeAsync(int id)
    {
        return await NativePartialOrNotFoundAsync(id, "_CreateFolioNative");
    }

    public async Task<IActionResult> OnGetReceiveDepositNativeAsync(int id)
    {
        if (!CanUseFinanceActions)
        {
            return Forbid();
        }

        return await NativePartialOrNotFoundAsync(id, "_ReceiveDepositNative");
    }

    public async Task<IActionResult> OnGetAllocateNativeAsync(int id)
    {
        if (!CanUseFinanceActions)
        {
            return Forbid();
        }

        return await NativePartialOrNotFoundAsync(id, "_AllocateNative");
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id)
    {
        var group = await context.GroupBookings.FindAsync(id);
        if (group is null)
        {
            return NotFound();
        }

        if (EditInput.DepartureDate.Date <= EditInput.ArrivalDate.Date)
        {
            ModelState.AddModelError("EditInput.DepartureDate", "Departure date must be after arrival date.");
        }

        if (EditInput.CreditLimit < 0 || EditInput.DepositAmount < 0)
        {
            ModelState.AddModelError(string.Empty, "Credit limit and deposit amount cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            return await NativePartialOrPageAsync(id, "_UpdateNative");
        }

        group.GroupName = EditInput.GroupName.Trim();
        group.BookingStatus = EditInput.BookingStatus;
        group.ContactPerson = EditInput.ContactPerson;
        group.ContactNumber = EditInput.ContactNumber;
        group.Email = EditInput.Email;
        group.ArrivalDate = EditInput.ArrivalDate.Date;
        group.DepartureDate = EditInput.DepartureDate.Date;
        group.MarketSegment = EditInput.MarketSegment;
        group.Source = EditInput.Source;
        group.BillingInstruction = EditInput.BillingInstruction;
        group.CreditLimit = EditInput.CreditLimit;
        group.DepositRequired = EditInput.DepositRequired;
        group.DepositAmount = EditInput.DepositAmount;
        group.Notes = EditInput.Notes;

        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Update, "Group Management", nameof(GroupBooking), group.Id.ToString(), null, new { group.GroupCode, group.GroupName, group.BookingStatus });
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddBlockAsync(int id)
    {
        var group = await context.GroupBookings.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (group is null)
        {
            return NotFound();
        }

        BlockInput.GroupBookingId = id;
        BlockInput.BlockDate = BlockInput.BlockDate.Date;
        BlockInput.CutOffDate = BlockInput.CutOffDate?.Date;

        if (BlockInput.BlockDate < group.ArrivalDate.Date || BlockInput.BlockDate >= group.DepartureDate.Date)
        {
            ModelState.AddModelError("BlockInput.BlockDate", "Block date must be within group stay dates.");
        }

        if (BlockInput.RoomsBlocked < 0 || BlockInput.RoomsPickedUp < 0 || BlockInput.RoomsReleased < 0)
        {
            ModelState.AddModelError(string.Empty, "Blocked, picked-up, and released rooms cannot be negative.");
        }

        if (BlockInput.RoomsPickedUp > BlockInput.RoomsBlocked)
        {
            ModelState.AddModelError("BlockInput.RoomsPickedUp", "Rooms picked up cannot exceed rooms blocked.");
        }

        if (BlockInput.RateAmount < 0)
        {
            ModelState.AddModelError("BlockInput.RateAmount", "Rate amount cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            return await NativePartialOrPageAsync(id, "_AddBlockNative");
        }

        context.GroupRoomBlocks.Add(BlockInput);
        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Create, "Group Management", nameof(GroupRoomBlock), BlockInput.Id.ToString(), null, new { BlockInput.GroupBookingId, BlockInput.RoomTypeId, BlockInput.BlockDate, BlockInput.RoomsBlocked });
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReleaseBlockAsync(int id, int blockId)
    {
        var block = await context.GroupRoomBlocks.FirstOrDefaultAsync(item => item.Id == blockId && item.GroupBookingId == id);
        if (block is not null)
        {
            block.RoomsReleased = Math.Max(block.RoomsReleased, block.RoomsBlocked - block.RoomsPickedUp);
            await context.SaveChangesAsync();
            await auditLogService.LogAsync(AuditActionType.Update, "Group Management", nameof(GroupRoomBlock), block.Id.ToString(), null, new { block.GroupBookingId, block.RoomsReleased });
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostLinkReservationAsync(int id)
    {
        MemberInput.GroupBookingId = id;
        if (MemberInput.ReservationId <= 0)
        {
            ModelState.AddModelError("MemberInput.ReservationId", "Reservation is required.");
        }

        if (await context.GroupMemberReservations.AnyAsync(item => item.GroupBookingId == id && item.ReservationId == MemberInput.ReservationId))
        {
            ModelState.AddModelError("MemberInput.ReservationId", "Reservation is already linked to this group.");
        }

        if (!ModelState.IsValid)
        {
            return await NativePartialOrPageAsync(id, "_LinkReservationNative");
        }

        context.GroupMemberReservations.Add(MemberInput);

        var reservation = await context.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == MemberInput.ReservationId);
        if (reservation is not null)
        {
            var blockDates = Enumerable.Range(0, Math.Max(0, (reservation.DepartureDate.Date - reservation.ArrivalDate.Date).Days))
                .Select(offset => reservation.ArrivalDate.Date.AddDays(offset))
                .ToList();

            var matchingBlocks = await context.GroupRoomBlocks
                .Where(item =>
                    item.GroupBookingId == id &&
                    item.RoomTypeId == reservation.RoomTypeId &&
                    blockDates.Contains(item.BlockDate.Date))
                .ToListAsync();

            foreach (var block in matchingBlocks)
            {
                var availableForPickup = Math.Max(0, block.RoomsBlocked - block.RoomsReleased);
                if (block.RoomsPickedUp < availableForPickup)
                {
                    block.RoomsPickedUp++;
                }
            }
        }

        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Create, "Group Management", nameof(GroupMemberReservation), MemberInput.Id.ToString(), null, new { MemberInput.GroupBookingId, MemberInput.ReservationId, MemberInput.BillingRoutingType });
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPickupReservationAsync(int id)
    {
        var group = await context.GroupBookings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);
        if (group is null)
        {
            return NotFound();
        }

        var block = await context.GroupRoomBlocks
            .Include(item => item.RoomType)
            .FirstOrDefaultAsync(item => item.Id == PickupInput.GroupRoomBlockId && item.GroupBookingId == id);
        if (block is null)
        {
            ModelState.AddModelError("PickupInput.GroupRoomBlockId", "Select a valid room block.");
        }

        if (PickupInput.GuestId <= 0)
        {
            ModelState.AddModelError("PickupInput.GuestId", "Guest is required.");
        }

        if (PickupInput.Adults <= 0)
        {
            ModelState.AddModelError("PickupInput.Adults", "At least one adult is required.");
        }

        var remainingPickup = block is null ? 0 : block.RoomsBlocked - block.RoomsPickedUp - block.RoomsReleased;
        if (remainingPickup <= 0)
        {
            ModelState.AddModelError("PickupInput.GroupRoomBlockId", "This room block has no remaining pickup inventory.");
        }

        Room? selectedRoom = null;
        if (PickupInput.RoomId is not null)
        {
            selectedRoom = await context.Rooms.AsNoTracking().FirstOrDefaultAsync(item => item.Id == PickupInput.RoomId.Value);
            if (selectedRoom is null)
            {
                ModelState.AddModelError("PickupInput.RoomId", "The selected room was not found.");
            }
            else if (block is not null && selectedRoom.RoomTypeId != block.RoomTypeId)
            {
                ModelState.AddModelError("PickupInput.RoomId", "Selected room must match the room block room type.");
            }
            else if (!selectedRoom.IsActive || selectedRoom.Status is RoomStatus.Occupied or RoomStatus.Dirty or RoomStatus.OutOfOrder or RoomStatus.Maintenance)
            {
                ModelState.AddModelError("PickupInput.RoomId", $"Room {selectedRoom.RoomNumber} is {selectedRoom.Status} and cannot be assigned to a group pickup reservation.");
            }
            else
            {
                var hasConflict = await context.Reservations.AsNoTracking().AnyAsync(reservation =>
                    reservation.RoomId == selectedRoom.Id &&
                    reservation.Status != ReservationStatus.Cancelled &&
                    reservation.Status != ReservationStatus.CheckedOut &&
                    reservation.Status != ReservationStatus.NoShow &&
                    reservation.ArrivalDate.Date < group.DepartureDate.Date &&
                    reservation.DepartureDate.Date > group.ArrivalDate.Date);
                if (hasConflict)
                {
                    ModelState.AddModelError("PickupInput.RoomId", "The selected room already has an active reservation during the group stay.");
                }
            }
        }

        if (!ModelState.IsValid || block is null)
        {
            return await NativePartialOrPageAsync(id, "_PickupNative");
        }

        var reservation = new Reservation
        {
            PropertyId = selectedRoom?.PropertyId ?? await context.RoomTypes
                .Where(item => item.Id == block.RoomTypeId)
                .Select(item => item.PropertyId)
                .FirstOrDefaultAsync(),
            GuestId = PickupInput.GuestId,
            RoomTypeId = block.RoomTypeId,
            RoomId = selectedRoom?.Id,
            RatePlanId = block.RatePlanId,
            ConfirmationNumber = CreateGroupPickupConfirmationNumber(id),
            ArrivalDate = group.ArrivalDate.Date,
            DepartureDate = group.DepartureDate.Date,
            RateAmount = PickupInput.RateAmount > 0 ? PickupInput.RateAmount : block.RateAmount,
            Adults = PickupInput.Adults,
            Children = PickupInput.Children,
            Status = ReservationStatus.Reserved,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Reservations.Add(reservation);
        block.RoomsPickedUp++;
        await context.SaveChangesAsync();

        var member = new GroupMemberReservation
        {
            GroupBookingId = id,
            ReservationId = reservation.Id,
            IsPrimaryGuest = PickupInput.IsPrimaryGuest,
            BillingRoutingType = PickupInput.BillingRoutingType,
            Notes = string.IsNullOrWhiteSpace(PickupInput.Notes) ? $"Picked up from block {block.Id}." : PickupInput.Notes
        };
        context.GroupMemberReservations.Add(member);
        await context.SaveChangesAsync();

        await auditLogService.LogAsync(AuditActionType.Create, "Group Management", nameof(GroupMemberReservation), member.Id.ToString(), null, new { member.GroupBookingId, member.ReservationId, member.BillingRoutingType, block.Id });
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCreateFolioAsync(int id)
    {
        GroupFolioInput.GroupBookingId = id;
        GroupFolioInput.CreatedBy = User.Identity?.Name ?? "System";
        GroupFolioInput.CreatedAt = DateTime.Now;

        if (string.IsNullOrWhiteSpace(GroupFolioInput.FolioName))
        {
            ModelState.AddModelError("GroupFolioInput.FolioName", "Folio name is required.");
        }

        if (string.IsNullOrWhiteSpace(GroupFolioInput.BillingName))
        {
            ModelState.AddModelError("GroupFolioInput.BillingName", "Billing name is required.");
        }

        if (!ModelState.IsValid)
        {
            return await NativePartialOrPageAsync(id, "_CreateFolioNative");
        }

        context.GroupFolios.Add(GroupFolioInput);
        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Create, "Group Management", nameof(GroupFolio), GroupFolioInput.Id.ToString(), null, new { GroupFolioInput.GroupBookingId, GroupFolioInput.FolioName, GroupFolioInput.Status });
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReceiveDepositAsync(int id)
    {
        if (!CanUseFinanceActions)
        {
            return Forbid();
        }

        DepositInput.GroupBookingId = id;
        DepositInput.ReceivedBy = string.IsNullOrWhiteSpace(DepositInput.ReceivedBy) ? User.Identity?.Name ?? "Finance" : DepositInput.ReceivedBy;
        DepositInput.DepositDate = DepositInput.DepositDate.Date;

        if (DepositInput.Amount <= 0)
        {
            ModelState.AddModelError("DepositInput.Amount", "Deposit amount must be positive.");
        }

        if (string.IsNullOrWhiteSpace(DepositInput.PaymentMethod))
        {
            ModelState.AddModelError("DepositInput.PaymentMethod", "Payment method is required.");
        }

        if (!ModelState.IsValid)
        {
            return await NativePartialOrPageAsync(id, "_ReceiveDepositNative");
        }

        context.GroupDeposits.Add(DepositInput);
        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Create, "Group Management", nameof(GroupDeposit), DepositInput.Id.ToString(), null, new { DepositInput.GroupBookingId, DepositInput.Amount, DepositInput.PaymentMethod, DepositInput.Status });
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAllocateAsync(int id)
    {
        if (!CanUseFinanceActions)
        {
            return Forbid();
        }

        AllocationInput.GroupBookingId = id;
        AllocationInput.AllocationDate = AllocationInput.AllocationDate.Date;
        AllocationInput.AllocatedBy = string.IsNullOrWhiteSpace(AllocationInput.AllocatedBy) ? User.Identity?.Name ?? "Finance" : AllocationInput.AllocatedBy;

        if (AllocationInput.GroupDepositId is null && AllocationInput.PaymentId is null)
        {
            ModelState.AddModelError(string.Empty, "Select a deposit or payment source.");
        }

        if (AllocationInput.TargetFolioId is null && AllocationInput.TargetReservationId is null)
        {
            ModelState.AddModelError(string.Empty, "Select a target folio or reservation.");
        }

        if (AllocationInput.AllocatedAmount <= 0)
        {
            ModelState.AddModelError("AllocationInput.AllocatedAmount", "Allocated amount must be positive.");
        }

        if (AllocationInput.GroupDepositId is not null)
        {
            var available = await groupManagementService.GetDepositAvailableAmountAsync(AllocationInput.GroupDepositId.Value);
            if (AllocationInput.AllocatedAmount > available)
            {
                ModelState.AddModelError("AllocationInput.AllocatedAmount", $"Allocation exceeds available deposit amount ({available:C}).");
            }
        }

        int? validatedTargetFolioId = AllocationInput.TargetFolioId;
        if (validatedTargetFolioId is null && AllocationInput.TargetReservationId is not null)
        {
            validatedTargetFolioId = await context.Folios
                .Where(item => item.ReservationId == AllocationInput.TargetReservationId.Value && item.Status == FolioStatus.Open)
                .OrderBy(item => item.Id)
                .Select(item => (int?)item.Id)
                .FirstOrDefaultAsync();
            if (validatedTargetFolioId is null)
            {
                ModelState.AddModelError("AllocationInput.TargetReservationId", "The selected reservation has no open folio to receive this allocation.");
            }
        }

        if (!ModelState.IsValid)
        {
            return await NativePartialOrPageAsync(id, "_AllocateNative");
        }

        context.GroupPaymentAllocations.Add(AllocationInput);
        var deposit = AllocationInput.GroupDepositId is null ? null : await context.GroupDeposits.FindAsync(AllocationInput.GroupDepositId.Value);
        var targetFolioId = validatedTargetFolioId;

        if (targetFolioId is not null && deposit is not null)
        {
            context.Payments.Add(new Payment
            {
                FolioId = targetFolioId.Value,
                Amount = AllocationInput.AllocatedAmount,
                PaymentMethod = $"Group Deposit - {deposit.PaymentMethod}",
                PaymentDate = AllocationInput.AllocationDate.Date.Add(DateTime.Now.TimeOfDay),
                ReferenceNumber = deposit.ReferenceNumber,
                Notes = $"Allocated from group deposit {deposit.Id}. {AllocationInput.Notes}",
                Status = PaymentStatus.Completed
            });
        }

        if (deposit is not null)
        {
            var availableAfter = await groupManagementService.GetDepositAvailableAmountAsync(deposit.Id) - AllocationInput.AllocatedAmount;
            if (availableAfter <= 0)
            {
                deposit.Status = GroupDepositStatus.Applied;
            }
        }

        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Create, "Group Management", nameof(GroupPaymentAllocation), AllocationInput.Id.ToString(), null, new { AllocationInput.GroupBookingId, AllocationInput.GroupDepositId, AllocationInput.TargetFolioId, AllocationInput.TargetReservationId, AllocationInput.AllocatedAmount });
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult> NativePartialOrNotFoundAsync(int id, string partialName)
    {
        var result = await LoadOrNotFoundAsync(id);
        return result is NotFoundResult ? result : NativePartial(partialName);
    }

    private async Task<IActionResult> NativePartialOrPageAsync(int id, string partialName)
    {
        var result = await LoadOrNotFoundAsync(id);
        if (result is NotFoundResult)
        {
            return result;
        }

        return IsNativeWorkflowRequest() ? NativePartial(partialName) : Page();
    }

    private bool IsNativeWorkflowRequest()
    {
        return string.Equals(Request.Query["vpmsNative"], "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-VPMS-Native-Dialog"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private PartialViewResult NativePartial(string partialName)
    {
        return new PartialViewResult
        {
            ViewName = partialName,
            ViewData = new ViewDataDictionary<DetailsModel>(ViewData, this)
        };
    }

    public async Task<IActionResult> OnPostCloseGroupFolioAsync(int id, int groupFolioId)
    {
        if (!CanUseFinanceActions)
        {
            return Forbid();
        }

        var groupFolio = await context.GroupFolios.FirstOrDefaultAsync(item => item.Id == groupFolioId && item.GroupBookingId == id);
        if (groupFolio is not null && groupFolio.Status == GroupFolioStatus.Open)
        {
            groupFolio.Status = GroupFolioStatus.Closed;
            await context.SaveChangesAsync();
            await auditLogService.LogAsync(AuditActionType.Update, "Group Management", nameof(GroupFolio), groupFolio.Id.ToString(), null, new { groupFolio.Status });
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostTransferGroupFolioToArAsync(int id, int groupFolioId)
    {
        if (!CanUseFinanceActions)
        {
            return Forbid();
        }

        var groupFolio = await context.GroupFolios.FirstOrDefaultAsync(item => item.Id == groupFolioId && item.GroupBookingId == id);
        if (groupFolio is not null && groupFolio.Status is GroupFolioStatus.Open or GroupFolioStatus.Closed or GroupFolioStatus.Billed)
        {
            groupFolio.Status = GroupFolioStatus.TransferredToAR;
            groupFolio.Notes = string.IsNullOrWhiteSpace(groupFolio.Notes)
                ? "Marked for AR transfer. Review and create AR invoice through finance workflow."
                : $"{groupFolio.Notes} Marked for AR transfer.";
            await context.SaveChangesAsync();
            await auditLogService.LogAsync(AuditActionType.Update, "Group Management", nameof(GroupFolio), groupFolio.Id.ToString(), null, new { groupFolio.Status, groupFolio.Notes });
        }

        return RedirectToPage(new { id });
    }

    private async Task<IActionResult> LoadOrNotFoundAsync(int id)
    {
        var group = await context.GroupBookings
            .Include(item => item.SalesAccount)
            .Include(item => item.RoomBlocks).ThenInclude(item => item.RoomType)
            .Include(item => item.RoomBlocks).ThenInclude(item => item.RatePlan)
            .Include(item => item.Members).ThenInclude(item => item.Reservation).ThenInclude(item => item!.Guest)
            .Include(item => item.Members).ThenInclude(item => item.Reservation).ThenInclude(item => item!.Room)
            .Include(item => item.Members).ThenInclude(item => item.Reservation).ThenInclude(item => item!.RoomType)
            .Include(item => item.GroupFolios).ThenInclude(item => item.PseudoRoom)
            .Include(item => item.GroupFolios).ThenInclude(item => item.Folio).ThenInclude(item => item!.Items)
            .Include(item => item.GroupFolios).ThenInclude(item => item.Folio).ThenInclude(item => item!.Payments)
            .Include(item => item.Deposits)
            .Include(item => item.PaymentAllocations)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (group is null)
        {
            return NotFound();
        }

        GroupBooking = group;
        EditInput = new GroupBooking
        {
            GroupName = group.GroupName,
            BookingStatus = group.BookingStatus,
            ContactPerson = group.ContactPerson,
            ContactNumber = group.ContactNumber,
            Email = group.Email,
            ArrivalDate = group.ArrivalDate,
            DepartureDate = group.DepartureDate,
            MarketSegment = group.MarketSegment,
            Source = group.Source,
            BillingInstruction = group.BillingInstruction,
            CreditLimit = group.CreditLimit,
            DepositRequired = group.DepositRequired,
            DepositAmount = group.DepositAmount,
            Notes = group.Notes
        };

        if (BlockInput.BlockDate == default)
        {
            BlockInput.BlockDate = group.ArrivalDate.Date;
        }

        if (DepositInput.DepositDate == default)
        {
            DepositInput.DepositDate = DateTime.Today;
        }

        if (AllocationInput.AllocationDate == default)
        {
            AllocationInput.AllocationDate = DateTime.Today;
        }

        if (PickupInput.Adults <= 0)
        {
            PickupInput.Adults = 1;
        }

        CollectionSummary = await groupManagementService.GetCollectionSummaryAsync(id);
        await LoadOptionsAsync(id);
        return Page();
    }

    private async Task LoadOptionsAsync(int groupBookingId)
    {
        RoomTypeOptions = new SelectList(await context.RoomTypes.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new { item.Id, Name = item.Code + " - " + item.Name }).ToListAsync(), "Id", "Name");
        RatePlanOptions = new SelectList(await context.RatePlans.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new { item.Id, Name = item.Code + " - " + item.Name }).ToListAsync(), "Id", "Name");
        ReservationOptions = new SelectList(await context.Reservations.AsNoTracking().Include(item => item.Guest).OrderByDescending(item => item.ArrivalDate).Select(item => new { item.Id, Name = item.ConfirmationNumber + " - " + item.Guest!.FirstName + " " + item.Guest.LastName }).ToListAsync(), "Id", "Name");
        PseudoRoomOptions = new SelectList(await context.PseudoRooms.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.PseudoRoomCode).Select(item => new { item.Id, Name = item.PseudoRoomCode + " - " + item.PseudoRoomName }).ToListAsync(), "Id", "Name");
        FolioOptions = new SelectList(await context.Folios.AsNoTracking().Include(item => item.Guest).OrderByDescending(item => item.Id).Select(item => new { item.Id, Name = item.FolioNumber + " - " + item.Guest!.FirstName + " " + item.Guest.LastName }).ToListAsync(), "Id", "Name");
        var deposits = await context.GroupDeposits
            .AsNoTracking()
            .Where(item => item.GroupBookingId == groupBookingId && item.Status != GroupDepositStatus.Cancelled)
            .OrderByDescending(item => item.DepositDate)
            .Select(item => new { item.Id, item.DepositDate, item.Amount, item.PaymentMethod })
            .ToListAsync();
        DepositOptions = new SelectList(deposits.Select(item => new { item.Id, Name = $"{item.DepositDate:yyyy-MM-dd} - {item.Amount:C} - {item.PaymentMethod}" }), "Id", "Name");
        MemberReservationOptions = new SelectList(await context.GroupMemberReservations.AsNoTracking().Include(item => item.Reservation).ThenInclude(item => item!.Guest).Where(item => item.GroupBookingId == groupBookingId).Select(item => new { item.ReservationId, Name = item.Reservation!.ConfirmationNumber + " - " + item.Reservation.Guest!.FirstName + " " + item.Reservation.Guest.LastName }).ToListAsync(), "ReservationId", "Name");
        var blockRows = await context.GroupRoomBlocks
            .AsNoTracking()
            .Include(item => item.RoomType)
            .Where(item => item.GroupBookingId == groupBookingId)
            .OrderBy(item => item.BlockDate)
            .Select(item => new
            {
                item.Id,
                item.BlockDate,
                RoomTypeCode = item.RoomType!.Code,
                Remaining = item.RoomsBlocked - item.RoomsPickedUp - item.RoomsReleased
            })
            .ToListAsync();
        BlockOptions = new SelectList(blockRows.Select(item => new { item.Id, Name = $"{item.BlockDate:yyyy-MM-dd} - {item.RoomTypeCode} ({item.Remaining} remaining)" }), "Id", "Name");
        GuestOptions = new SelectList(await context.Guests.AsNoTracking().OrderBy(item => item.LastName).ThenBy(item => item.FirstName).Select(item => new { item.Id, Name = item.LastName + ", " + item.FirstName }).ToListAsync(), "Id", "Name");
        RoomOptions = new SelectList(await context.Rooms
            .AsNoTracking()
            .Include(item => item.RoomType)
            .Where(item =>
                item.IsActive &&
                (item.Status == RoomStatus.Available ||
                    item.Status == RoomStatus.Clean ||
                    item.Status == RoomStatus.Inspected))
            .OrderBy(item => item.RoomNumber)
            .Select(item => new { item.Id, Name = item.RoomNumber + " - " + item.RoomType!.Code + " (" + item.Status + ")" })
            .ToListAsync(), "Id", "Name");
    }

    private static string CreateGroupPickupConfirmationNumber(int groupBookingId)
    {
        return $"GRP-{groupBookingId:0000}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}

public class GroupPickupInput
{
    public int GroupRoomBlockId { get; set; }

    public int GuestId { get; set; }

    public int? RoomId { get; set; }

    public decimal RateAmount { get; set; }

    public int Adults { get; set; } = 1;

    public int Children { get; set; }

    public bool IsPrimaryGuest { get; set; }

    public BillingRoutingType BillingRoutingType { get; set; } = BillingRoutingType.RoomAndTaxToMaster;

    public string? Notes { get; set; }
}
