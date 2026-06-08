using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Banquet;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Models.Executive;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public class DataValidationService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<int> RunValidationScanAsync(string scannedBy)
    {
        var issueCount = 0;
        issueCount += await ScanReservationsAsync();
        issueCount += await ScanRoomsAsync();
        issueCount += await ScanFoliosAsync();
        issueCount += await ScanFoodBeverageAsync();
        issueCount += await ScanBanquetAsync();
        issueCount += await ScanPurchasingInventoryAsync();
        issueCount += await ScanFinanceAndArAsync();
        issueCount += await ScanAccountingAsync();
        issueCount += await ScanPhilippineReportControlsAsync();
        issueCount += await ScanLaborCostingAsync();
        issueCount += await ScanGuestPortalPrivacyAsync();
        issueCount += await ScanExecutiveReportingAsync();
        issueCount += await ScanRevenueAndBookingAsync();
        issueCount += await ScanGroupManagementAsync();
        await _context.SaveChangesAsync();
        return issueCount;
    }

    public async Task MarkResolvedAsync(int id, string resolvedBy)
    {
        var issue = await _context.DataValidationIssues.FindAsync(id);
        if (issue is null)
        {
            return;
        }

        issue.IsResolved = true;
        issue.ResolvedBy = resolvedBy;
        issue.ResolvedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    private async Task<int> ScanReservationsAsync()
    {
        var count = 0;
        var reservationsWithoutGuest = await _context.Reservations
            .AsNoTracking()
            .Where(reservation => !_context.Guests.Any(guest => guest.Id == reservation.GuestId))
            .Select(reservation => reservation.Id)
            .ToListAsync();
        foreach (var id in reservationsWithoutGuest)
        {
            count += await AddIssueAsync("Front Office", nameof(Reservation), id, DataValidationIssueType.OrphanRecord, SystemSeverity.High, "Reservation has no valid guest record.", "Assign or recreate the reservation with a valid guest.");
        }

        var reservationsWithoutRoomType = await _context.Reservations
            .AsNoTracking()
            .Where(reservation =>
                (reservation.Status == ReservationStatus.Reserved || reservation.Status == ReservationStatus.CheckedIn) &&
                reservation.RoomId == null &&
                reservation.RoomTypeId == null)
            .Select(reservation => reservation.Id)
            .ToListAsync();
        foreach (var id in reservationsWithoutRoomType)
        {
            count += await AddIssueAsync("Front Office", nameof(Reservation), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "Active reservation has neither room nor room type assigned.", "Assign a room type or physical room before arrival.");
        }

        var checkedInWrongRoomStatus = await _context.Reservations
            .AsNoTracking()
            .Where(reservation =>
                reservation.Status == ReservationStatus.CheckedIn &&
                reservation.RoomId != null &&
                reservation.Room != null &&
                reservation.Room.Status != RoomStatus.Occupied)
            .Select(reservation => reservation.Id)
            .ToListAsync();
        foreach (var id in checkedInWrongRoomStatus)
        {
            count += await AddIssueAsync("Front Office", nameof(Reservation), id, DataValidationIssueType.InvalidStatus, SystemSeverity.High, "Checked-in reservation room is not marked Occupied.", "Set the assigned room status to Occupied or review the reservation status.");
        }

        var checkedOutWrongRoomStatus = await _context.Reservations
            .AsNoTracking()
            .Where(reservation =>
                reservation.Status == ReservationStatus.CheckedOut &&
                reservation.RoomId != null &&
                reservation.Room != null &&
                reservation.Room.Status != RoomStatus.Dirty &&
                reservation.Room.Status != RoomStatus.Clean &&
                reservation.Room.Status != RoomStatus.Available)
            .Select(reservation => reservation.Id)
            .ToListAsync();
        foreach (var id in checkedOutWrongRoomStatus)
        {
            count += await AddIssueAsync("Front Office", nameof(Reservation), id, DataValidationIssueType.InvalidStatus, SystemSeverity.Medium, "Checked-out reservation room has an unexpected status.", "Review the room status and return it to dirty, clean, or available.");
        }

        return count;
    }

    private async Task<int> ScanRoomsAsync()
    {
        var count = 0;
        var occupiedRoomsWithoutReservation = await _context.Rooms
            .AsNoTracking()
            .Where(room =>
                room.Status == RoomStatus.Occupied &&
                !_context.Reservations.Any(reservation =>
                    reservation.RoomId == room.Id &&
                    reservation.Status == ReservationStatus.CheckedIn))
            .Select(room => room.Id)
            .ToListAsync();

        foreach (var id in occupiedRoomsWithoutReservation)
        {
            count += await AddIssueAsync("Front Office", nameof(Room), id, DataValidationIssueType.InvalidStatus, SystemSeverity.High, "Room is marked Occupied without a checked-in reservation.", "Assign the in-house reservation or correct the room status.");
        }

        return count;
    }

    private async Task<int> ScanFoliosAsync()
    {
        var count = 0;
        var today = DateTime.Today;
        var folioIssues = await _context.Folios
            .AsNoTracking()
            .Where(folio =>
                !_context.Reservations.Any(reservation => reservation.Id == folio.ReservationId) ||
                !_context.Guests.Any(guest => guest.Id == folio.GuestId))
            .Select(folio => folio.Id)
            .ToListAsync();
        foreach (var id in folioIssues)
        {
            count += await AddIssueAsync("Finance", nameof(Folio), id, DataValidationIssueType.OrphanRecord, SystemSeverity.High, "Folio has missing reservation or guest link.", "Review the folio and restore valid reservation and guest references.");
        }

        var folioBalances = await _context.Folios
            .AsNoTracking()
            .Select(folio => new
            {
                folio.Id,
                Balance = (_context.FolioItems
                    .Where(item => item.FolioId == folio.Id && !item.IsVoided)
                    .Sum(item => (decimal?)item.Amount) ?? 0) -
                    (_context.Payments
                        .Where(payment => payment.FolioId == folio.Id && payment.Status != PaymentStatus.Voided && payment.Status != PaymentStatus.Failed)
                        .Sum(payment => (decimal?)payment.Amount) ?? 0)
            })
            .ToListAsync();
        foreach (var folio in folioBalances.Where(folio => folio.Balance < 0))
        {
            count += await AddIssueAsync("Finance", nameof(Folio), folio.Id, DataValidationIssueType.InconsistentBalance, SystemSeverity.Medium, "Folio has a negative balance.", "Review payments, refunds, discounts, and adjustments on this folio.");
        }

        var paymentIssues = await _context.Payments
            .AsNoTracking()
            .Where(payment => !_context.Folios.Any(folio => folio.Id == payment.FolioId))
            .Select(payment => payment.Id)
            .ToListAsync();
        foreach (var id in paymentIssues)
        {
            count += await AddIssueAsync("Finance", nameof(Payment), id, DataValidationIssueType.OrphanRecord, SystemSeverity.High, "Payment has no valid folio.", "Link the payment to a valid folio or void it through finance controls.");
        }

        var paymentsWithoutCashierTrace = await _context.Payments
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Completed && !payment.CashierTransactions.Any())
            .Select(payment => new { payment.Id, payment.Notes })
            .ToListAsync();
        foreach (var payment in paymentsWithoutCashierTrace)
        {
            var managementPosted = payment.Notes?.Contains("Management-posted without open cashier shift", StringComparison.OrdinalIgnoreCase) == true;
            count += await AddIssueAsync(
                "Finance",
                nameof(Payment),
                payment.Id,
                DataValidationIssueType.MissingRequiredData,
                managementPosted ? SystemSeverity.Medium : SystemSeverity.High,
                managementPosted
                    ? "Payment was management-posted without cashier shift trace."
                    : "Completed payment has no linked cashier transaction.",
                managementPosted
                    ? "Review management-posted payments during cashier audit."
                    : "Review receipt and cashier shift history; void and repost if the payment was not authorized.");
        }

        var referencePayments = await _context.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.Status != PaymentStatus.Voided &&
                payment.Status != PaymentStatus.Failed &&
                payment.ReferenceNumber != null &&
                payment.ReferenceNumber != "")
            .Select(payment => new
            {
                payment.Id,
                payment.FolioId,
                payment.ReferenceNumber
            })
            .ToListAsync();
        var duplicateReferencePayments = referencePayments
            .GroupBy(payment => new
            {
                payment.FolioId,
                Reference = payment.ReferenceNumber!.Trim().ToUpperInvariant()
            })
            .Where(group => !string.IsNullOrWhiteSpace(group.Key.Reference) && group.Count() > 1)
            .SelectMany(group => group)
            .ToList();
        foreach (var payment in duplicateReferencePayments)
        {
            count += await AddIssueAsync("Finance", nameof(Payment), payment.Id, DataValidationIssueType.DuplicateRecord, SystemSeverity.High, "Payment reference is duplicated on the same folio.", "Confirm whether this is a duplicate receipt, processor retry, or valid split settlement.");
        }

        var recentCutoff = today.AddDays(-14);
        var recentPayments = await _context.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.Status != PaymentStatus.Voided &&
                payment.Status != PaymentStatus.Failed &&
                payment.PaymentDate >= recentCutoff)
            .Select(payment => new
            {
                payment.Id,
                payment.FolioId,
                payment.Amount,
                payment.PaymentMethod,
                payment.PaymentDate
            })
            .ToListAsync();
        var repeatSubmissionPayments = recentPayments
            .GroupBy(payment => new
            {
                payment.FolioId,
                payment.Amount,
                Method = NormalizePaymentMethodForValidation(payment.PaymentMethod),
                Minute = new DateTime(payment.PaymentDate.Year, payment.PaymentDate.Month, payment.PaymentDate.Day, payment.PaymentDate.Hour, payment.PaymentDate.Minute, 0)
            })
            .Where(group => group.Count() > 1)
            .SelectMany(group => group)
            .ToList();
        foreach (var payment in repeatSubmissionPayments)
        {
            count += await AddIssueAsync("Finance", nameof(Payment), payment.Id, DataValidationIssueType.DuplicateRecord, SystemSeverity.High, "Similar payment was posted to the same folio within the same minute.", "Review the receipts before accepting further settlement on this folio.");
        }

        var paymentsAfterFolioClose = (await _context.Payments
                .AsNoTracking()
                .Where(payment =>
                    payment.Status == PaymentStatus.Completed &&
                    payment.Folio != null &&
                    payment.Folio.ClosedAtUtc != null)
                .Select(payment => new
                {
                    payment.Id,
                    payment.PaymentDate,
                    payment.Folio!.ClosedAtUtc
                })
                .ToListAsync())
            .Where(payment => payment.ClosedAtUtc is not null && payment.PaymentDate.ToUniversalTime() > payment.ClosedAtUtc.Value)
            .Select(payment => payment.Id)
            .ToList();
        foreach (var id in paymentsAfterFolioClose)
        {
            count += await AddIssueAsync("Finance", nameof(Payment), id, DataValidationIssueType.InvalidStatus, SystemSeverity.Medium, "Payment was posted after the folio closed.", "Confirm whether this was an approved post-checkout collection or should be handled through AR/city ledger controls.");
        }

        var invalidPaymentAmounts = await _context.Payments
            .AsNoTracking()
            .Where(payment => payment.Amount <= 0 && payment.Status != PaymentStatus.Voided && payment.Status != PaymentStatus.Failed)
            .Select(payment => payment.Id)
            .ToListAsync();
        foreach (var id in invalidPaymentAmounts)
        {
            count += await AddIssueAsync("Finance", nameof(Payment), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.High, "Payment amount is zero or negative.", "Review and void the payment if it was not posted through an approved adjustment workflow.");
        }

        var invalidFolioItems = await _context.FolioItems
            .AsNoTracking()
            .Where(item =>
                !item.IsVoided &&
                item.Amount <= 0 &&
                !item.ChargeCode.StartsWith("DISC") &&
                !item.ChargeCode.StartsWith("ADJ") &&
                (item.ChargeCodeDefinition == null ||
                    item.ChargeCodeDefinition.ChargeCategory != ChargeCategory.Discount &&
                    item.ChargeCodeDefinition.ChargeCategory != ChargeCategory.Adjustment))
            .Select(item => item.Id)
            .ToListAsync();
        foreach (var id in invalidFolioItems)
        {
            count += await AddIssueAsync("Finance", nameof(FolioItem), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.Medium, "Folio item has zero or negative amount outside discount/adjustment handling.", "Review the charge amount, charge code, or void the item through finance controls.");
        }

        return count;
    }

    private async Task<int> ScanFoodBeverageAsync()
    {
        var count = 0;
        var closedUnpaidOrders = await _context.POSOrders
            .AsNoTracking()
            .Where(order => order.OrderStatus == POSOrderStatus.Closed && order.PaymentStatus == POSPaymentStatus.Unpaid)
            .Select(order => order.Id)
            .ToListAsync();
        foreach (var id in closedUnpaidOrders)
        {
            count += await AddIssueAsync("F&B Service", nameof(POSOrder), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.Medium, "POS order is closed but unpaid.", "Settle, charge to room, or correct the order payment status.");
        }

        var itemIssues = await _context.POSOrderItems
            .AsNoTracking()
            .Where(item => !_context.MenuItems.Any(menuItem => menuItem.Id == item.MenuItemId))
            .Select(item => item.Id)
            .ToListAsync();
        foreach (var id in itemIssues)
        {
            count += await AddIssueAsync("F&B Service", nameof(POSOrderItem), id, DataValidationIssueType.OrphanRecord, SystemSeverity.High, "POS order item has no valid menu item.", "Restore the menu item or void the order item.");
        }

        return count;
    }

    private async Task<int> ScanBanquetAsync()
    {
        var count = 0;
        var eventsWithoutBeo = await _context.BanquetEvents
            .AsNoTracking()
            .Where(banquetEvent =>
                banquetEvent.EventStatus == BanquetEventStatus.Confirmed &&
                banquetEvent.BanquetEventOrder == null)
            .Select(banquetEvent => banquetEvent.Id)
            .ToListAsync();
        foreach (var id in eventsWithoutBeo)
        {
            count += await AddIssueAsync("Banquet", nameof(BanquetEvent), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Confirmed banquet event has no BEO.", "Create and approve a BEO before event operations.");
        }

        return count;
    }

    private async Task<int> ScanPurchasingInventoryAsync()
    {
        var count = 0;
        var approvedPoWithoutItems = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(order =>
                order.Status == PurchaseOrderStatus.Approved &&
                !order.Items.Any())
            .Select(order => order.Id)
            .ToListAsync();
        foreach (var id in approvedPoWithoutItems)
        {
            count += await AddIssueAsync("Purchasing", nameof(PurchaseOrder), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Approved purchase order has no items.", "Add purchase order items or cancel the purchase order.");
        }

        var postedReceivingWithoutMovement = await _context.ReceivingRecords
            .AsNoTracking()
            .Where(receiving =>
                receiving.Status == ReceivingStatus.Posted &&
                !_context.StockMovements.Any(movement =>
                    movement.ReferenceType == nameof(ReceivingRecord) &&
                    movement.ReferenceId == receiving.Id))
            .Select(receiving => receiving.Id)
            .ToListAsync();
        foreach (var id in postedReceivingWithoutMovement)
        {
            count += await AddIssueAsync("Inventory", nameof(ReceivingRecord), id, DataValidationIssueType.OrphanRecord, SystemSeverity.High, "Posted receiving record has no stock movement.", "Review receiving and stock movement history before adjusting inventory.");
        }

        var negativeStock = await _context.InventoryItems
            .AsNoTracking()
            .Where(item => item.CurrentStock < 0)
            .Select(item => item.Id)
            .ToListAsync();
        foreach (var id in negativeStock)
        {
            count += await AddIssueAsync("Inventory", nameof(InventoryItem), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.High, "Inventory item has negative stock.", "Run a stock adjustment after verifying physical count.");
        }

        return count;
    }

    private async Task<int> ScanFinanceAndArAsync()
    {
        var count = 0;
        var today = DateTime.Today;
        var negativeArInvoices = await _context.ARInvoices
            .AsNoTracking()
            .Where(invoice => invoice.Balance < 0)
            .Select(invoice => invoice.Id)
            .ToListAsync();
        foreach (var id in negativeArInvoices)
        {
            count += await AddIssueAsync("Accounts Receivable", nameof(ARInvoice), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.High, "AR invoice has a negative balance.", "Review allocations, credit memos, debit memos, and invoice totals.");
        }

        var unallocatedArPayments = await _context.ARPayments
            .AsNoTracking()
            .Select(payment => new
            {
                payment.Id,
                payment.Amount,
                Allocated = payment.Allocations.Sum(allocation => allocation.AllocatedAmount)
            })
            .Where(payment => payment.Amount > payment.Allocated)
            .Select(payment => payment.Id)
            .ToListAsync();
        foreach (var id in unallocatedArPayments)
        {
            count += await AddIssueAsync("Accounts Receivable", nameof(ARPayment), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.Medium, "AR payment has unapplied balance.", "Allocate the payment to open invoices or document the unapplied balance.");
        }

        var overdueCutoff = today.AddDays(-30);
        var overdueAccountsWithoutRecentPayment = await _context.ARInvoices
            .AsNoTracking()
            .Where(invoice =>
                invoice.Balance > 0 &&
                invoice.DueDate.Date < today &&
                invoice.Status != ARInvoiceStatus.Cancelled &&
                invoice.Status != ARInvoiceStatus.WrittenOff &&
                !_context.ARPayments.Any(payment => payment.ARAccountId == invoice.ARAccountId && payment.PaymentDate >= overdueCutoff))
            .Select(invoice => invoice.ARAccountId)
            .Distinct()
            .ToListAsync();
        foreach (var id in overdueAccountsWithoutRecentPayment)
        {
            count += await AddIssueAsync("Accounts Receivable", nameof(ARAccount), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "AR account has overdue balance and no payment in the last 30 days.", "Prioritize collection follow-up or update collection notes.");
        }

        var unappliedCreditMemos = await _context.CreditMemos
            .AsNoTracking()
            .Where(memo => memo.Status == MemoStatus.Approved)
            .Select(memo => memo.Id)
            .ToListAsync();
        foreach (var id in unappliedCreditMemos)
        {
            count += await AddIssueAsync("Accounts Receivable", nameof(CreditMemo), id, DataValidationIssueType.InvalidStatus, SystemSeverity.Medium, "Approved credit memo has not been applied.", "Apply the credit memo to the invoice or cancel it after review.");
        }

        var unappliedDebitMemos = await _context.DebitMemos
            .AsNoTracking()
            .Where(memo => memo.Status == MemoStatus.Approved)
            .Select(memo => memo.Id)
            .ToListAsync();
        foreach (var id in unappliedDebitMemos)
        {
            count += await AddIssueAsync("Accounts Receivable", nameof(DebitMemo), id, DataValidationIssueType.InvalidStatus, SystemSeverity.Medium, "Approved debit memo has not been applied.", "Apply the debit memo to the invoice or cancel it after review.");
        }

        var accountBalanceMismatches = await _context.ARAccounts
            .AsNoTracking()
            .Select(account => new
            {
                account.Id,
                account.CurrentBalance,
                InvoiceBalance = account.Invoices
                    .Where(invoice =>
                        invoice.Status != ARInvoiceStatus.Paid &&
                        invoice.Status != ARInvoiceStatus.Cancelled &&
                        invoice.Status != ARInvoiceStatus.WrittenOff)
                    .Sum(invoice => invoice.Balance)
            })
            .Where(account => Math.Abs(account.CurrentBalance - account.InvoiceBalance) > 0.01m)
            .Select(account => account.Id)
            .ToListAsync();
        foreach (var id in accountBalanceMismatches)
        {
            count += await AddIssueAsync("Accounts Receivable", nameof(ARAccount), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.Medium, "AR account balance does not match open invoice balance.", "Recalculate the AR account balance and review invoice allocations.");
        }

        var businessDate = await _context.BusinessDateSettings
            .AsNoTracking()
            .Select(setting => (DateTime?)setting.CurrentBusinessDate)
            .FirstOrDefaultAsync() ?? today;
        var previousOpenShifts = await _context.CashierShifts
            .AsNoTracking()
            .Where(shift => shift.Status == CashierShiftStatus.Open && shift.BusinessDate < businessDate.Date)
            .Select(shift => shift.Id)
            .ToListAsync();
        foreach (var id in previousOpenShifts)
        {
            count += await AddIssueAsync("Finance", nameof(CashierShift), id, DataValidationIssueType.InvalidStatus, SystemSeverity.Critical, "Cashier shift from a previous business date is still open.", "Close or audit the cashier shift before night audit.");
        }

        return count;
    }

    private async Task<int> ScanRevenueAndBookingAsync()
    {
        var count = 0;
        var activeRoomTypesWithoutRates = await _context.RoomTypes
            .AsNoTracking()
            .Where(roomType =>
                roomType.IsActive &&
                !_context.RoomTypeRates.Any(rate => rate.RoomTypeId == roomType.Id && rate.IsActive))
            .Select(roomType => roomType.Id)
            .ToListAsync();
        foreach (var id in activeRoomTypesWithoutRates)
        {
            count += await AddIssueAsync("Revenue", nameof(RoomType), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Active room type has no active rate.", "Create an active room type rate for this room type.");
        }

        var convertedBookingMissingReservation = await _context.BookingRequests
            .AsNoTracking()
            .Where(request =>
                request.BookingStatus == BookingRequestStatus.ConvertedToReservation &&
                request.ReservationId == null)
            .Select(request => request.Id)
            .ToListAsync();
        foreach (var id in convertedBookingMissingReservation)
        {
            count += await AddIssueAsync("Booking Engine", nameof(BookingRequest), id, DataValidationIssueType.OrphanRecord, SystemSeverity.High, "Booking request is marked converted but has no reservation link.", "Link the reservation or correct the booking request status.");
        }

        return count;
    }

    private async Task<int> ScanGroupManagementAsync()
    {
        var count = 0;

        var activeGroupsWithoutFolio = await _context.GroupBookings
            .AsNoTracking()
            .Where(group =>
                group.BookingStatus != GroupBookingStatus.Inquiry &&
                group.BookingStatus != GroupBookingStatus.Cancelled &&
                group.BookingStatus != GroupBookingStatus.NoShow &&
                !group.GroupFolios.Any(folio => folio.Status != GroupFolioStatus.Cancelled))
            .Select(group => group.Id)
            .ToListAsync();
        foreach (var id in activeGroupsWithoutFolio)
        {
            count += await AddIssueAsync("Group Management", nameof(GroupBooking), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "Active group booking has no group master folio.", "Create a group master folio or document why billing remains guest-pay-own.");
        }

        var confirmedGroupsWithoutBlocks = await _context.GroupBookings
            .AsNoTracking()
            .Where(group =>
                group.BookingStatus == GroupBookingStatus.Confirmed &&
                !group.RoomBlocks.Any())
            .Select(group => group.Id)
            .ToListAsync();
        foreach (var id in confirmedGroupsWithoutBlocks)
        {
            count += await AddIssueAsync("Group Management", nameof(GroupBooking), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Confirmed group booking has no room block.", "Create room blocks by room type and date before pickup or forecast reporting.");
        }

        var pickupExceedingBlock = await _context.GroupRoomBlocks
            .AsNoTracking()
            .Where(block => block.RoomsPickedUp > block.RoomsBlocked)
            .Select(block => block.Id)
            .ToListAsync();
        foreach (var id in pickupExceedingBlock)
        {
            count += await AddIssueAsync("Group Management", nameof(GroupRoomBlock), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.High, "Group room pickup exceeds rooms blocked.", "Review group pickup counts and release or adjust the room block.");
        }

        var negativeGroupFolios = await _context.GroupFolios
            .AsNoTracking()
            .Where(groupFolio => groupFolio.FolioId != null)
            .Select(groupFolio => new
            {
                groupFolio.Id,
                Balance = (_context.FolioItems
                    .Where(item => item.FolioId == groupFolio.FolioId && !item.IsVoided)
                    .Sum(item => (decimal?)item.Amount) ?? 0) -
                    (_context.Payments
                        .Where(payment => payment.FolioId == groupFolio.FolioId && payment.Status != PaymentStatus.Voided && payment.Status != PaymentStatus.Failed)
                        .Sum(payment => (decimal?)payment.Amount) ?? 0)
            })
            .Where(item => item.Balance < 0)
            .Select(item => item.Id)
            .ToListAsync();
        foreach (var id in negativeGroupFolios)
        {
            count += await AddIssueAsync("Group Management", nameof(GroupFolio), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.Medium, "Group master folio has a negative balance.", "Review deposits, payments, allocations, and routed charges before closing the group folio.");
        }

        var physicalRoomsMatchingPseudoRooms = await _context.PseudoRooms
            .AsNoTracking()
            .Where(pseudo => _context.Rooms.Any(room => room.RoomNumber == pseudo.PseudoRoomCode))
            .Select(pseudo => pseudo.Id)
            .ToListAsync();
        foreach (var id in physicalRoomsMatchingPseudoRooms)
        {
            count += await AddIssueAsync("Group Management", nameof(PseudoRoom), id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.Medium, "Pseudo room code also exists as a physical room number.", "Keep pseudo room/paymaster accounts separate from physical room inventory.");
        }

        var inactivePseudoRoomsWithOpenFolios = await _context.PseudoRooms
            .AsNoTracking()
            .Where(pseudo => !pseudo.IsActive && pseudo.GroupFolios.Any(folio => folio.Status == GroupFolioStatus.Open))
            .Select(pseudo => pseudo.Id)
            .ToListAsync();
        foreach (var id in inactivePseudoRoomsWithOpenFolios)
        {
            count += await AddIssueAsync("Group Management", nameof(PseudoRoom), id, DataValidationIssueType.InvalidStatus, SystemSeverity.Medium, "Inactive pseudo room has an open folio.", "Close, transfer, or reactivate the pseudo room before continuing group billing.");
        }

        var invalidRoutingRules = await _context.ChargeRoutingRules
            .AsNoTracking()
            .Where(rule => rule.IsActive &&
                ((rule.RouteToType == RouteToType.GroupMasterFolio &&
                    (rule.TargetGroupFolioId == null || !_context.GroupFolios.Any(folio => folio.Id == rule.TargetGroupFolioId && folio.Status != GroupFolioStatus.Cancelled))) ||
                 (rule.RouteToType == RouteToType.GuestFolio &&
                    (rule.TargetFolioId == null || !_context.Folios.Any(folio => folio.Id == rule.TargetFolioId))) ||
                 (rule.RouteToType == RouteToType.PseudoRoomFolio &&
                    rule.TargetGroupFolioId == null &&
                    (rule.TargetPseudoRoomId == null || !_context.PseudoRooms.Any(pseudo => pseudo.Id == rule.TargetPseudoRoomId && pseudo.IsActive)))))
            .Select(rule => rule.Id)
            .ToListAsync();
        foreach (var id in invalidRoutingRules)
        {
            count += await AddIssueAsync("Group Management", nameof(ChargeRoutingRule), id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.High, "Charge routing rule has an invalid or missing target.", "Update the routing target to a valid folio, group master folio, or active pseudo room.");
        }

        var completedGroups = await _context.GroupBookings
            .AsNoTracking()
            .Where(group => group.BookingStatus == GroupBookingStatus.Completed)
            .Select(group => group.Id)
            .ToListAsync();
        foreach (var groupId in completedGroups)
        {
            var unappliedDeposits = await _context.GroupDeposits
                .AsNoTracking()
                .Where(deposit =>
                    deposit.GroupBookingId == groupId &&
                    deposit.Status == GroupDepositStatus.Received &&
                    ((_context.GroupPaymentAllocations
                        .Where(allocation => allocation.GroupDepositId == deposit.Id)
                        .Sum(allocation => (decimal?)allocation.AllocatedAmount) ?? 0) < deposit.Amount))
                .Select(deposit => deposit.Id)
                .ToListAsync();

            foreach (var id in unappliedDeposits)
            {
                count += await AddIssueAsync("Group Management", nameof(GroupDeposit), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.Medium, "Completed group has an unapplied deposit.", "Allocate, refund, forfeit, or cancel the remaining deposit balance.");
            }
        }

        return count;
    }

    private async Task<int> ScanAccountingAsync()
    {
        var count = 0;
        var unbalancedEntries = await _context.JournalEntries
            .AsNoTracking()
            .Where(entry => entry.Status == JournalEntryStatus.Posted)
            .Select(entry => new
            {
                entry.Id,
                Lines = entry.Lines.Count,
                Debit = entry.Lines.Sum(line => line.DebitAmount),
                Credit = entry.Lines.Sum(line => line.CreditAmount)
            })
            .Where(entry => entry.Lines < 2 || entry.Debit != entry.Credit)
            .ToListAsync();

        foreach (var entry in unbalancedEntries)
        {
            count += await AddIssueAsync("Accounting", nameof(JournalEntry), entry.Id, DataValidationIssueType.InconsistentBalance, SystemSeverity.Critical, "Posted journal entry is not balanced or has fewer than two lines.", "Reverse and repost the journal entry after accounting review.");
        }

        var journalLinesWithoutAccounts = await _context.JournalEntryLines
            .AsNoTracking()
            .Where(line => !_context.GLAccounts.Any(account => account.Id == line.GLAccountId))
            .Select(line => line.Id)
            .ToListAsync();
        foreach (var id in journalLinesWithoutAccounts)
        {
            count += await AddIssueAsync("Accounting", nameof(JournalEntryLine), id, DataValidationIssueType.OrphanRecord, SystemSeverity.Critical, "Journal entry line has no valid GL account.", "Assign a valid active GL account.");
        }

        var invalidPostingRules = await _context.PostingRules
            .AsNoTracking()
            .Where(rule =>
                rule.IsActive &&
                (!_context.GLAccounts.Any(account => account.Id == rule.DebitGLAccountId && account.IsActive) ||
                    !_context.GLAccounts.Any(account => account.Id == rule.CreditGLAccountId && account.IsActive)))
            .Select(rule => rule.Id)
            .ToListAsync();
        foreach (var id in invalidPostingRules)
        {
            count += await AddIssueAsync("Accounting", nameof(PostingRule), id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.High, "Active posting rule has missing or inactive debit/credit GL account.", "Update the posting rule with valid active GL accounts.");
        }

        var inactiveAccountsInUse = await _context.JournalEntryLines
            .AsNoTracking()
            .Where(line => line.GLAccount != null && !line.GLAccount.IsActive)
            .Select(line => line.GLAccountId)
            .Distinct()
            .ToListAsync();
        foreach (var id in inactiveAccountsInUse)
        {
            count += await AddIssueAsync("Accounting", nameof(GLAccount), id, DataValidationIssueType.InvalidStatus, SystemSeverity.High, "Inactive GL account is used in journal entries.", "Reactivate the account or review historical journal usage.");
        }

        var duplicateSourcePostings = await _context.JournalEntries
            .AsNoTracking()
            .Where(entry => entry.Status == JournalEntryStatus.Posted && entry.SourceReferenceId != null)
            .GroupBy(entry => new { entry.SourceModule, entry.SourceTransactionType, entry.SourceReferenceId })
            .Where(group => group.Count() > 1)
            .Select(group => group.Key.SourceReferenceId)
            .ToListAsync();
        foreach (var id in duplicateSourcePostings)
        {
            count += await AddIssueAsync("Accounting", nameof(JournalEntry), id, DataValidationIssueType.DuplicateRecord, SystemSeverity.High, "Source transaction has duplicate posted journal entries.", "Review duplicate posting and reverse incorrect entries.");
        }

        var periods = await _context.AccountingPeriods.AsNoTracking().OrderBy(period => period.StartDate).ToListAsync();
        foreach (var period in periods)
        {
            var overlaps = periods.Any(other => other.Id != period.Id && period.StartDate <= other.EndDate && period.EndDate >= other.StartDate);
            if (overlaps)
            {
                count += await AddIssueAsync("Accounting", nameof(AccountingPeriod), period.Id, DataValidationIssueType.DateConflict, SystemSeverity.High, "Accounting period overlaps with another period.", "Adjust accounting period date ranges.");
            }
        }

        var apInvoiceTotals = await _context.APInvoices
            .AsNoTracking()
            .Select(invoice => new
            {
                invoice.Id,
                invoice.TotalAmount,
                invoice.Balance,
                invoice.Status,
                invoice.JournalEntryId,
                LineTotal = invoice.Lines.Sum(line => line.Quantity * line.UnitCost) + invoice.Lines.Sum(line => line.TaxAmount) - invoice.DiscountAmount
            })
            .ToListAsync();
        foreach (var invoice in apInvoiceTotals.Where(invoice => Math.Abs(invoice.TotalAmount - invoice.LineTotal) > 0.01m))
        {
            count += await AddIssueAsync("Accounts Payable", nameof(APInvoice), invoice.Id, DataValidationIssueType.InconsistentBalance, SystemSeverity.High, "AP invoice total does not match invoice line totals.", "Recalculate the AP invoice and review tax, withholding tax, and discounts.");
        }

        foreach (var invoice in apInvoiceTotals.Where(invoice => invoice.Balance < 0))
        {
            count += await AddIssueAsync("Accounts Payable", nameof(APInvoice), invoice.Id, DataValidationIssueType.InconsistentBalance, SystemSeverity.High, "AP invoice has a negative balance.", "Review payment vouchers and invoice totals.");
        }

        foreach (var invoice in apInvoiceTotals.Where(invoice =>
            (invoice.Status is APInvoiceStatus.Approved or APInvoiceStatus.PartiallyPaid or APInvoiceStatus.Paid) &&
            invoice.JournalEntryId == null))
        {
            count += await AddIssueAsync("Accounts Payable", nameof(APInvoice), invoice.Id, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Approved AP invoice has no journal entry.", "Post the AP invoice to the general ledger or review approval history.");
        }

        var releasedVouchersWithoutJournal = await _context.PaymentVouchers
            .AsNoTracking()
            .Where(voucher => voucher.Status == PaymentVoucherStatus.Released && voucher.JournalEntryId == null)
            .Select(voucher => voucher.Id)
            .ToListAsync();
        foreach (var id in releasedVouchersWithoutJournal)
        {
            count += await AddIssueAsync("Accounts Payable", nameof(PaymentVoucher), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Released payment voucher has no journal entry.", "Review the voucher release and GL posting.");
        }

        var approvedReconsWithDifference = await _context.BankReconciliations
            .AsNoTracking()
            .Where(reconciliation => reconciliation.Status == BankReconciliationStatus.Approved && reconciliation.Difference != 0)
            .Select(reconciliation => reconciliation.Id)
            .ToListAsync();
        foreach (var id in approvedReconsWithDifference)
        {
            count += await AddIssueAsync("Banking", nameof(BankReconciliation), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.Critical, "Approved bank reconciliation has a non-zero difference.", "Reopen review and correct bank reconciliation items.");
        }

        var manualBankTransactionsWithoutJournal = await _context.BankTransactions
            .AsNoTracking()
            .Where(transaction => transaction.SourceModule == SourceModule.Manual && transaction.JournalEntryId == null)
            .Select(transaction => transaction.Id)
            .ToListAsync();
        foreach (var id in manualBankTransactionsWithoutJournal)
        {
            count += await AddIssueAsync("Banking", nameof(BankTransaction), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Manual bank transaction has no posted journal entry.", "Create a GL-backed bank transaction or reverse the unsupported manual entry.");
        }

        var approvedReconAdjustmentsWithoutBankTransaction = await _context.BankReconciliationItems
            .AsNoTracking()
            .Where(item =>
                item.BankReconciliation != null &&
                item.BankReconciliation.Status == BankReconciliationStatus.Approved &&
                item.BankTransactionId == null &&
                item.IsCleared)
            .Select(item => item.Id)
            .ToListAsync();
        foreach (var id in approvedReconAdjustmentsWithoutBankTransaction)
        {
            count += await AddIssueAsync("Banking", nameof(BankReconciliationItem), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Approved bank reconciliation has a cleared adjustment without bank transaction support.", "Post the adjustment through GL-backed bank transaction workflow before approval.");
        }

        var duplicateSupplierInvoices = await _context.APInvoices
            .AsNoTracking()
            .Where(invoice => invoice.SupplierInvoiceNumber != null && invoice.SupplierInvoiceNumber != "" && invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
            .GroupBy(invoice => new { invoice.SupplierId, invoice.SupplierInvoiceNumber })
            .Where(group => group.Count() > 1)
            .Select(group => group.Min(invoice => invoice.Id))
            .ToListAsync();
        foreach (var id in duplicateSupplierInvoices)
        {
            count += await AddIssueAsync("Accounts Payable", nameof(APInvoice), id, DataValidationIssueType.DuplicateRecord, SystemSeverity.High, "Supplier has duplicate invoice/reference numbers.", "Review supplier invoice records and void/cancel duplicates.");
        }

        var apInvoicesMissingSupplierReference = await _context.APInvoices
            .AsNoTracking()
            .Where(invoice => invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided && (invoice.SupplierInvoiceNumber == null || invoice.SupplierInvoiceNumber == ""))
            .Select(invoice => invoice.Id)
            .ToListAsync();
        foreach (var id in apInvoicesMissingSupplierReference)
        {
            count += await AddIssueAsync("Accounts Payable", nameof(APInvoice), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "AP invoice is missing supplier invoice/reference number.", "Enter the supplier invoice or reference number for duplicate vendor-bill control.");
        }

        var duplicatePurchaseOrderInvoices = await _context.APInvoices
            .AsNoTracking()
            .Where(invoice => invoice.PurchaseOrderId != null && invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
            .GroupBy(invoice => invoice.PurchaseOrderId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Min(invoice => invoice.Id))
            .ToListAsync();
        foreach (var id in duplicatePurchaseOrderInvoices)
        {
            count += await AddIssueAsync("Accounts Payable", nameof(APInvoice), id, DataValidationIssueType.DuplicateRecord, SystemSeverity.Critical, "Multiple active AP invoices are linked to the same purchase order.", "Cancel or void duplicate AP invoices or implement controlled partial invoicing.");
        }

        var duplicateReceivingInvoices = await _context.APInvoices
            .AsNoTracking()
            .Where(invoice => invoice.ReceivingRecordId != null && invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
            .GroupBy(invoice => invoice.ReceivingRecordId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Min(invoice => invoice.Id))
            .ToListAsync();
        foreach (var id in duplicateReceivingInvoices)
        {
            count += await AddIssueAsync("Accounts Payable", nameof(APInvoice), id, DataValidationIssueType.DuplicateRecord, SystemSeverity.Critical, "Multiple active AP invoices are linked to the same receiving record.", "Cancel or void duplicate AP invoices or implement controlled partial invoicing.");
        }

        var closedPeriodsWithUnpostedAp = await _context.AccountingPeriods
            .AsNoTracking()
            .Where(period => period.Status == AccountingPeriodStatus.Closed &&
                _context.APInvoices.Any(invoice => invoice.InvoiceDate >= period.StartDate && invoice.InvoiceDate <= period.EndDate && invoice.Status == APInvoiceStatus.Approved && invoice.JournalEntryId == null))
            .Select(period => period.Id)
            .ToListAsync();
        foreach (var id in closedPeriodsWithUnpostedAp)
        {
            count += await AddIssueAsync("Accounting", nameof(AccountingPeriod), id, DataValidationIssueType.InvalidStatus, SystemSeverity.High, "Closed accounting period has unposted AP transactions.", "Review AP invoice posting before locking the period.");
        }

        var activeCashAccountIds = await _context.CashAccountSettings
            .AsNoTracking()
            .Where(setting => setting.IsActive && setting.GLAccount != null && setting.GLAccount.IsActive)
            .Select(setting => setting.GLAccountId)
            .ToListAsync();
        if (activeCashAccountIds.Count == 0)
        {
            count += await AddIssueAsync("Accounting", nameof(CashAccountSetting), 0, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Cash accounts are not configured for Statement of Cash Flows reporting.", "Configure Cash Account Settings for cash on hand, cash in bank, e-wallet, and cash equivalents.");
        }

        if (!await _context.CashFlowCategories.AsNoTracking().AnyAsync(category => category.IsActive))
        {
            count += await AddIssueAsync("Accounting", nameof(CashFlowCategory), 0, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Cash flow categories are missing.", "Seed or configure operating, investing, financing, and reconciliation cash flow categories.");
        }

        var invalidCashFlowRules = await _context.CashFlowMappingRules
            .AsNoTracking()
            .Where(rule =>
                rule.IsActive &&
                rule.GLAccountId != null &&
                !_context.GLAccounts.Any(account => account.Id == rule.GLAccountId.Value && account.IsActive))
            .Select(rule => rule.Id)
            .ToListAsync();
        foreach (var id in invalidCashFlowRules)
        {
            count += await AddIssueAsync("Accounting", nameof(CashFlowMappingRule), id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.High, "Active cash flow mapping rule points to a missing or inactive GL account.", "Update the cash flow mapping rule with a valid active GL account or deactivate the rule.");
        }

        if (activeCashAccountIds.Count > 0)
        {
            var cashFlowRules = await _context.CashFlowMappingRules.AsNoTracking().Where(rule => rule.IsActive).ToListAsync();
            var recentCashEntries = await _context.JournalEntries
                .AsNoTracking()
                .Include(entry => entry.Lines)
                .Where(entry => entry.Status == JournalEntryStatus.Posted && entry.Lines.Any(line => activeCashAccountIds.Contains(line.GLAccountId)))
                .OrderByDescending(entry => entry.JournalDate)
                .Take(100)
                .ToListAsync();

            foreach (var entry in recentCashEntries)
            {
                var sourceMapped = cashFlowRules.Any(rule =>
                    rule.SourceModule == entry.SourceModule &&
                    (rule.SourceTransactionType == entry.SourceTransactionType || rule.SourceTransactionType == null));
                var offsetMapped = entry.Lines
                    .Where(line => !activeCashAccountIds.Contains(line.GLAccountId))
                    .Any(line => cashFlowRules.Any(rule => rule.GLAccountId == line.GLAccountId));

                if (!sourceMapped && !offsetMapped)
                {
                    count += await AddIssueAsync("Accounting", nameof(JournalEntry), entry.Id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "Posted cash journal entry is not mapped to a cash flow category.", "Create a Cash Flow Mapping Rule for the source transaction or offset GL account.");
                }
            }
        }

        return count;
    }

    private async Task<int> ScanPhilippineReportControlsAsync()
    {
        var count = 0;

        if (!await _context.PhilippineTaxReportLines.AsNoTracking().AnyAsync(line => line.IsActive))
        {
            count += await AddIssueAsync("Philippine Reports", nameof(PhilippineTaxReportLine), 0, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "No active Philippine tax report lines are configured.", "Configure Philippine tax report lines before relying on tax-support reports for finance review.");
        }

        var invalidReportLineAccounts = await _context.PhilippineTaxReportLines
            .AsNoTracking()
            .Where(line =>
                line.IsActive &&
                line.GLAccountId != null &&
                !_context.GLAccounts.Any(account => account.Id == line.GLAccountId.Value && account.IsActive))
            .Select(line => line.Id)
            .ToListAsync();
        foreach (var id in invalidReportLineAccounts)
        {
            count += await AddIssueAsync("Philippine Reports", nameof(PhilippineTaxReportLine), id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.High, "Active Philippine tax report line points to a missing or inactive GL account.", "Update the report line with a valid active GL account or deactivate the line.");
        }

        var invalidReportLineTaxCodes = await _context.PhilippineTaxReportLines
            .AsNoTracking()
            .Where(line =>
                line.IsActive &&
                line.TaxCodeId != null &&
                !_context.TaxCodes.Any(taxCode => taxCode.Id == line.TaxCodeId.Value && taxCode.IsActive))
            .Select(line => line.Id)
            .ToListAsync();
        foreach (var id in invalidReportLineTaxCodes)
        {
            count += await AddIssueAsync("Philippine Reports", nameof(PhilippineTaxReportLine), id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.High, "Active Philippine tax report line points to a missing or inactive tax code.", "Update the report line with a valid active tax code or deactivate the line.");
        }

        var activeTaxCodesWithoutValidGl = await _context.TaxCodes
            .AsNoTracking()
            .Where(taxCode =>
                taxCode.IsActive &&
                taxCode.TaxType != TaxType.Other &&
                (taxCode.GLAccountId == null ||
                    !_context.GLAccounts.Any(account => account.Id == taxCode.GLAccountId.Value && account.IsActive)))
            .Select(taxCode => taxCode.Id)
            .ToListAsync();
        foreach (var id in activeTaxCodesWithoutValidGl)
        {
            count += await AddIssueAsync("Philippine Reports", nameof(TaxCode), id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.High, "Active VAT/withholding tax code has no valid GL account mapping.", "Map the tax code to an active GL control account before using Philippine tax-support reports.");
        }

        var accountsWithUnmappedPhilippineCategory = await _context.GLAccounts
            .AsNoTracking()
            .Where(account =>
                account.IsActive &&
                account.PhilippineReportCategory != null &&
                account.PhilippineReportCategory != "" &&
                !_context.PhilippineTaxReportLines.Any(line => line.IsActive && line.GLAccountId == account.Id))
            .Select(account => account.Id)
            .ToListAsync();
        foreach (var id in accountsWithUnmappedPhilippineCategory)
        {
            count += await AddIssueAsync("Philippine Reports", nameof(GLAccount), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Low, "GL account has a Philippine report category but no active Philippine tax report line.", "Review whether this account should be mapped to an active Philippine tax-support report line.");
        }

        return count;
    }

    private async Task<int> ScanLaborCostingAsync()
    {
        var count = 0;
        var periods = await _context.PayrollPeriods.AsNoTracking().Where(period => period.Status != PayrollPeriodStatus.Cancelled).OrderBy(period => period.StartDate).ToListAsync();
        foreach (var period in periods)
        {
            if (periods.Any(other => other.Id != period.Id && period.StartDate <= other.EndDate && period.EndDate >= other.StartDate))
            {
                count += await AddIssueAsync("Labor Costing", nameof(PayrollPeriod), period.Id, DataValidationIssueType.DateConflict, SystemSeverity.High, "Payroll period overlaps with another active payroll period.", "Review payroll period date ranges before posting payroll cost.");
            }
        }

        var postedPayrollWithoutJournal = await _context.PayrollPeriods
            .AsNoTracking()
            .Where(period => period.Status == PayrollPeriodStatus.Posted && period.JournalEntryId == null)
            .Select(period => period.Id)
            .ToListAsync();
        foreach (var id in postedPayrollWithoutJournal)
        {
            count += await AddIssueAsync("Labor Costing", nameof(PayrollPeriod), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.High, "Posted payroll period has no journal entry.", "Review payroll posting and repost or correct the period.");
        }

        var payrollEntriesMissingMapping = await _context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry => entry.DepartmentId == null && entry.USALIDepartmentId == null)
            .Select(entry => entry.Id)
            .ToListAsync();
        foreach (var id in payrollEntriesMissingMapping)
        {
            count += await AddIssueAsync("Labor Costing", nameof(PayrollCostEntry), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "Payroll cost entry has no Department or USALI Department mapping.", "Assign a department or USALI department before relying on department labor reports.");
        }

        var payrollEntriesMissingGl = await _context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry =>
                entry.LaborGLAccountId == null &&
                (entry.EmployeeCostProfile == null || entry.EmployeeCostProfile.DefaultLaborGLAccountId == null))
            .Select(entry => entry.Id)
            .ToListAsync();
        foreach (var id in payrollEntriesMissingGl)
        {
            count += await AddIssueAsync("Labor Costing", nameof(PayrollCostEntry), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "Payroll cost entry has no labor GL account mapping.", "Assign a labor GL account on the entry or employee cost profile.");
        }

        var approvedServiceChargePools = await _context.ServiceChargePools
            .AsNoTracking()
            .Where(pool => pool.Status == ServiceChargePoolStatus.Approved)
            .Select(pool => pool.Id)
            .ToListAsync();
        foreach (var id in approvedServiceChargePools)
        {
            count += await AddIssueAsync("Labor Costing", nameof(ServiceChargePool), id, DataValidationIssueType.InvalidStatus, SystemSeverity.Medium, "Service charge pool is approved but not posted.", "Post the approved service charge pool or cancel it before month-end close.");
        }

        var distributionMismatches = await _context.ServiceChargePools
            .AsNoTracking()
            .Where(pool => pool.Status != ServiceChargePoolStatus.Cancelled)
            .Select(pool => new
            {
                pool.Id,
                pool.TotalServiceChargeCollected,
                DistributionTotal = pool.DistributionLines.Sum(line => line.Amount)
            })
            .Where(pool => Math.Abs(pool.TotalServiceChargeCollected - pool.DistributionTotal) > 0.01m)
            .ToListAsync();
        foreach (var pool in distributionMismatches)
        {
            count += await AddIssueAsync("Labor Costing", nameof(ServiceChargePool), pool.Id, DataValidationIssueType.InconsistentBalance, SystemSeverity.High, "Service charge distribution total does not match pool total.", "Regenerate or manually adjust service charge distribution lines.");
        }

        var ineligibleServiceChargeLines = await _context.ServiceChargeDistributionLines
            .AsNoTracking()
            .Include(line => line.EmployeeCostProfile)
            .Include(line => line.ServiceChargePool)
            .Where(line =>
                line.EmployeeCostProfileId != null &&
                line.ServiceChargePool != null &&
                line.ServiceChargePool.Status != ServiceChargePoolStatus.Cancelled)
            .ToListAsync();
        foreach (var line in ineligibleServiceChargeLines.Where(line => !ServiceChargeEligibility.IsEligible(line.EmployeeCostProfile)))
        {
            count += await AddIssueAsync("Labor Costing", nameof(ServiceChargeDistributionLine), line.Id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.High, "Service charge distribution includes an inactive, agency, or managerial/executive employee profile.", "Review service-charge eligibility and regenerate or manually correct the distribution line before approval/posting.");
        }

        var employeeProfilesMissingMapping = await _context.EmployeeCostProfiles
            .AsNoTracking()
            .Where(employee => employee.IsActive && (employee.DepartmentId == null || employee.DefaultLaborGLAccountId == null))
            .Select(employee => employee.Id)
            .ToListAsync();
        foreach (var id in employeeProfilesMissingMapping)
        {
            count += await AddIssueAsync("Labor Costing", nameof(EmployeeCostProfile), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "Active employee cost profile is missing department or GL mapping.", "Complete the employee cost profile mapping before payroll posting.");
        }

        return count;
    }

    private async Task<int> ScanGuestPortalPrivacyAsync()
    {
        var count = 0;
        var now = DateTime.Now;

        var riskyPortalSettings = await _context.GuestPortalSettings
            .AsNoTracking()
            .Where(setting => setting.IsGuestPortalEnabled && !setting.RequireReservationLookupVerification)
            .Select(setting => setting.Id)
            .ToListAsync();
        foreach (var id in riskyPortalSettings)
        {
            count += await AddIssueAsync("Guest Portal", nameof(GuestPortalSetting), id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.High, "Guest portal lookup verification is disabled while the portal is enabled.", "Require email or phone verification before exposing stay details through the guest portal.");
        }

        var settingsWithoutPrivacyNotice = await _context.GuestPortalSettings
            .AsNoTracking()
            .Where(setting => setting.IsGuestPortalEnabled && (setting.PrivacyPolicy == null || setting.PrivacyPolicy == ""))
            .Select(setting => setting.Id)
            .ToListAsync();
        foreach (var id in settingsWithoutPrivacyNotice)
        {
            count += await AddIssueAsync("Guest Portal", nameof(GuestPortalSetting), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "Guest portal is enabled without a privacy policy notice.", "Add a hotel-reviewed privacy policy before trial use of guest self-service screens.");
        }

        var expiredActiveAccess = await _context.GuestPortalAccesses
            .AsNoTracking()
            .Where(access => access.IsActive && access.ExpiresAt != null && access.ExpiresAt < now)
            .Select(access => access.Id)
            .ToListAsync();
        foreach (var id in expiredActiveAccess)
        {
            count += await AddIssueAsync("Guest Portal", nameof(GuestPortalAccess), id, DataValidationIssueType.InvalidStatus, SystemSeverity.Medium, "Expired guest portal access token is still active.", "Deactivate expired access tokens during portal housekeeping.");
        }

        var accessWithoutVerificationContact = await _context.GuestPortalAccesses
            .AsNoTracking()
            .Where(access =>
                access.IsActive &&
                (access.GuestEmail == null || access.GuestEmail == "") &&
                (access.GuestPhone == null || access.GuestPhone == ""))
            .Select(access => access.Id)
            .ToListAsync();
        foreach (var id in accessWithoutVerificationContact)
        {
            count += await AddIssueAsync("Guest Portal", nameof(GuestPortalAccess), id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.High, "Active guest portal access has no email or phone verification contact.", "Expire the access token or restore verified guest contact details before continuing guest portal use.");
        }

        var longLivedAccess = (await _context.GuestPortalAccesses
                .AsNoTracking()
                .Where(access => access.IsActive && access.ExpiresAt != null)
                .Select(access => new { access.Id, access.CreatedAt, access.ExpiresAt })
                .ToListAsync())
            .Where(access => access.ExpiresAt!.Value.Date > access.CreatedAt.Date.AddDays(14))
            .Select(access => access.Id)
            .ToList();
        foreach (var id in longLivedAccess)
        {
            count += await AddIssueAsync("Guest Portal", nameof(GuestPortalAccess), id, DataValidationIssueType.SecurityConfiguration, SystemSeverity.Medium, "Guest portal access token lifetime is longer than the recommended trial limit.", "Shorten active guest portal access to 14 days or less unless management approves an exception.");
        }

        return count;
    }

    private async Task<int> AddIssueAsync(
        string module,
        string entityName,
        int? entityId,
        DataValidationIssueType issueType,
        SystemSeverity severity,
        string description,
        string recommendedFix)
    {
        var exists = await _context.DataValidationIssues.AnyAsync(issue =>
            !issue.IsResolved &&
            issue.Module == module &&
            issue.EntityName == entityName &&
            issue.EntityId == entityId &&
            issue.IssueType == issueType &&
            issue.Description == description);

        if (exists)
        {
            return 0;
        }

        _context.DataValidationIssues.Add(new DataValidationIssue
        {
            IssueDate = DateTime.Now,
            Module = module,
            EntityName = entityName,
            EntityId = entityId,
            IssueType = issueType,
            Severity = severity,
            Description = description,
            RecommendedFix = recommendedFix
        });

        return 1;
    }

    private static string NormalizePaymentMethodForValidation(string? paymentMethod)
    {
        return string.IsNullOrWhiteSpace(paymentMethod)
            ? string.Empty
            : paymentMethod.Replace("-", string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();
    }

    private async Task<int> ScanExecutiveReportingAsync()
    {
        var count = 0;

        var kpisMissingBenchmarks = await _context.ExecutiveKPIs
            .AsNoTracking()
            .Where(kpi => kpi.IsActive &&
                kpi.TargetValue == null &&
                !_context.KPIBenchmarkSettings.Any(setting => setting.IsActive && setting.KPIName == kpi.KPIName))
            .Select(kpi => kpi.Id)
            .ToListAsync();
        foreach (var id in kpisMissingBenchmarks)
        {
            count += await AddIssueAsync("Executive", nameof(ExecutiveKPI), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "Active executive KPI has no target or benchmark setting.", "Configure a KPI benchmark or default target before relying on KPI status.");
        }

        var operationalRevenueExists = await _context.FolioItems.AnyAsync(item => !item.IsVoided && item.Amount > 0) ||
            await _context.POSOrders.AnyAsync(order => order.TotalAmount > 0 && order.OrderStatus != POSOrderStatus.Cancelled);
        if (operationalRevenueExists)
        {
            var zeroRevenueSnapshotIds = await _context.ExecutiveReportSnapshots
                .AsNoTracking()
                .Where(snapshot => snapshot.TotalRevenue == 0)
                .Select(snapshot => snapshot.Id)
                .ToListAsync();
            foreach (var id in zeroRevenueSnapshotIds)
            {
                count += await AddIssueAsync("Executive", nameof(ExecutiveReportSnapshot), id, DataValidationIssueType.InconsistentBalance, SystemSeverity.Medium, "Executive snapshot has zero total revenue while operational revenue exists.", "Regenerate the executive snapshot after checking posting and report date filters.");
            }
        }

        var unmappedDepartmentSnapshots = await _context.DepartmentPerformanceSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.DepartmentId == null && snapshot.USALIDepartmentId == null)
            .Select(snapshot => snapshot.Id)
            .ToListAsync();
        foreach (var id in unmappedDepartmentSnapshots)
        {
            count += await AddIssueAsync("Executive", nameof(DepartmentPerformanceSnapshot), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "Department performance snapshot is not mapped to Department or USALI Department.", "Review department and USALI mappings for executive performance reporting.");
        }

        var packagesWithoutItems = await _context.OwnerReportPackages
            .AsNoTracking()
            .Where(package => !package.Items.Any(item => item.IsIncluded))
            .Select(package => package.Id)
            .ToListAsync();
        foreach (var id in packagesWithoutItems)
        {
            count += await AddIssueAsync("Executive", nameof(OwnerReportPackage), id, DataValidationIssueType.MissingRequiredData, SystemSeverity.Medium, "Owner report package has no included report items.", "Add at least one report section before printing or sending the package.");
        }

        var staleCriticalAlerts = await _context.ExecutiveAlerts
            .AsNoTracking()
            .Where(alert => !alert.IsResolved && alert.Severity == KPIStatus.Critical && alert.AlertDate < DateTime.Today.AddDays(-7))
            .Select(alert => alert.Id)
            .ToListAsync();
        foreach (var id in staleCriticalAlerts)
        {
            count += await AddIssueAsync("Executive", nameof(ExecutiveAlert), id, DataValidationIssueType.InvalidStatus, SystemSeverity.Critical, "Critical executive alert has been unresolved for more than 7 days.", "Escalate and resolve or document the alert disposition.");
        }

        return count;
    }
}
