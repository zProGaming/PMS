using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Core;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using NightAuditRecord = Vantage.PMS.Models.Finance.NightAudit;

namespace Vantage.PMS.Pages.Finance.NightAudit;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public string? CompletionNotes { get; set; }

    public DateTime BusinessDate { get; set; }

    public IList<ChecklistItem> Checklist { get; set; } = new List<ChecklistItem>();

    public IList<Reservation> UnresolvedArrivals { get; set; } = new List<Reservation>();

    public IList<Reservation> UnresolvedDepartures { get; set; } = new List<Reservation>();

    public IList<Folio> OutstandingFolios { get; set; } = new List<Folio>();

    public IList<Payment> OpenPayments { get; set; } = new List<Payment>();

    public IList<RoomIssue> RoomIssues { get; set; } = new List<RoomIssue>();

    public IList<ChecklistItem> FinanceWarnings { get; set; } = new List<ChecklistItem>();

    public IList<NightAuditRecord> AuditHistory { get; set; } = new List<NightAuditRecord>();

    public bool HasBlockingIssues => Checklist.Any(item => item.Count > 0);

    public async Task OnGetAsync()
    {
        await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostRunAsync()
    {
        var setting = await GetOrCreateBusinessDateSettingAsync();
        BusinessDate = setting.CurrentBusinessDate.Date;

        await LoadChecklistAsync(BusinessDate);

        if (HasBlockingIssues)
        {
            await LoadHistoryAsync();
            ModelState.AddModelError(string.Empty, "Night audit cannot run until all blocking issues are resolved.");
            return Page();
        }

        var startedAt = DateTime.Now;
        var lockedCharges = await LockChargesAsync(BusinessDate);
        var lockedPayments = await LockPaymentsAsync(BusinessDate);

        var audit = new NightAuditRecord
        {
            BusinessDate = BusinessDate,
            StartedAt = startedAt,
            CompletedAt = DateTime.Now,
            Status = NightAuditStatus.Completed,
            CompletedBy = User.Identity?.Name ?? Environment.UserName,
            Notes = BuildAuditSummary(BusinessDate, lockedCharges, lockedPayments, CompletionNotes)
        };

        _context.NightAudits.Add(audit);
        setting.CurrentBusinessDate = BusinessDate.AddDays(1);
        setting.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["NightAuditMessage"] = $"Night audit completed for {BusinessDate:d}. Business date advanced to {setting.CurrentBusinessDate:d}.";

        return RedirectToPage("./Index");
    }

    private async Task LoadPageAsync()
    {
        var setting = await GetOrCreateBusinessDateSettingAsync();
        BusinessDate = setting.CurrentBusinessDate.Date;

        await LoadChecklistAsync(BusinessDate);
        await LoadHistoryAsync();
    }

    private async Task<BusinessDateSetting> GetOrCreateBusinessDateSettingAsync()
    {
        var setting = await _context.BusinessDateSettings.FirstOrDefaultAsync();
        if (setting is not null)
        {
            return setting;
        }

        setting = new BusinessDateSetting
        {
            CurrentBusinessDate = DateTime.Today,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _context.BusinessDateSettings.Add(setting);
        await _context.SaveChangesAsync();

        return setting;
    }

    private async Task LoadChecklistAsync(DateTime businessDate)
    {
        var nextBusinessDate = businessDate.AddDays(1);

        UnresolvedArrivals = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .AsNoTracking()
            .Where(reservation =>
                reservation.ArrivalDate < nextBusinessDate &&
                (reservation.Status == ReservationStatus.Reserved || reservation.Status == ReservationStatus.Pending))
            .OrderBy(reservation => reservation.ArrivalDate)
            .ToListAsync();

        UnresolvedDepartures = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .AsNoTracking()
            .Where(reservation =>
                reservation.DepartureDate < nextBusinessDate &&
                reservation.Status == ReservationStatus.CheckedIn)
            .OrderBy(reservation => reservation.DepartureDate)
            .ToListAsync();

        var checkedInFolios = await _context.Folios
            .Include(folio => folio.Guest)
            .Include(folio => folio.Reservation)
                .ThenInclude(reservation => reservation!.Room)
            .Include(folio => folio.Items)
            .Include(folio => folio.Payments)
            .AsNoTracking()
            .Where(folio => folio.Reservation != null && folio.Reservation.Status == ReservationStatus.CheckedIn)
            .ToListAsync();

        OutstandingFolios = checkedInFolios
            .Where(folio => folio.Balance > 0)
            .OrderBy(folio => folio.Guest!.LastName)
            .ToList();

        OpenPayments = await _context.Payments
            .Include(payment => payment.Folio)
            .AsNoTracking()
            .Where(payment =>
                payment.PaymentDate >= businessDate &&
                payment.PaymentDate < nextBusinessDate &&
                payment.Status != PaymentStatus.Completed &&
                payment.Status != PaymentStatus.Voided &&
                payment.Status != PaymentStatus.Refunded)
            .OrderBy(payment => payment.PaymentDate)
            .ToListAsync();

        RoomIssues = await LoadRoomIssuesAsync(businessDate, nextBusinessDate);
        await LoadFinanceWarningsAsync(businessDate, nextBusinessDate);

        Checklist = new List<ChecklistItem>
        {
            new("Unresolved arrivals", UnresolvedArrivals.Count, "Reservations due to arrive but not checked in."),
            new("Unresolved departures", UnresolvedDepartures.Count, "Reservations due to depart but not checked out."),
            new("Outstanding balances", OutstandingFolios.Count, "Checked-in guests with folio balances greater than zero."),
            new("Open payments", OpenPayments.Count, "Payment records not completed, voided, or refunded for the business date."),
            new("Room status issues", RoomIssues.Count, "Rooms whose status conflicts with active reservation state.")
        };
    }

    private async Task LoadFinanceWarningsAsync(DateTime businessDate, DateTime nextBusinessDate)
    {
        var openCashierShifts = await _context.CashierShifts
            .CountAsync(shift => shift.BusinessDate >= businessDate && shift.BusinessDate < nextBusinessDate && shift.Status == CashierShiftStatus.Open);
        var unprocessedApprovedRefunds = await _context.RefundTransactions
            .CountAsync(refund => refund.Status == RefundStatus.Approved);
        var pendingVoidApprovals = await _context.VoidRequests
            .CountAsync(request => request.Status == ApprovalStatus.Pending);
        var pendingDiscountApprovals = await _context.DiscountApprovals
            .CountAsync(discount => discount.Status == ApprovalStatus.Pending);
        var unpostedFolioItems = await _context.FolioItems
            .CountAsync(item =>
                item.PostingDate >= businessDate &&
                item.PostingDate < nextBusinessDate &&
                !item.IsVoided &&
                !_context.JournalEntries.Any(entry =>
                    entry.Status == JournalEntryStatus.Posted &&
                    entry.SourceReferenceId == item.Id &&
                    (entry.SourceTransactionType == SourceTransactionType.FolioCharge ||
                        entry.SourceTransactionType == SourceTransactionType.RoomCharge)));
        var unpostedPayments = await _context.Payments
            .CountAsync(payment =>
                payment.PaymentDate >= businessDate &&
                payment.PaymentDate < nextBusinessDate &&
                payment.Status == PaymentStatus.Completed &&
                !_context.JournalEntries.Any(entry =>
                    entry.Status == JournalEntryStatus.Posted &&
                    entry.SourceReferenceId == payment.Id &&
                    entry.SourceTransactionType == SourceTransactionType.FolioPayment));
        var unpostedPosOrBanquet = await _context.POSOrders
            .CountAsync(order =>
                order.ClosedAt >= businessDate &&
                order.ClosedAt < nextBusinessDate &&
                order.OrderStatus == Vantage.PMS.Models.FoodBeverage.POSOrderStatus.Closed &&
                !_context.JournalEntries.Any(entry =>
                    entry.Status == JournalEntryStatus.Posted &&
                    entry.SourceReferenceId == order.Id &&
                    (entry.SourceTransactionType == SourceTransactionType.POSPayment ||
                        entry.SourceTransactionType == SourceTransactionType.POSChargeToRoom)))
            + await _context.BanquetCharges
                .CountAsync(charge =>
                    charge.ChargeDate >= businessDate &&
                    charge.ChargeDate < nextBusinessDate &&
                    !charge.IsVoided &&
                    !_context.JournalEntries.Any(entry =>
                        entry.Status == JournalEntryStatus.Posted &&
                        entry.SourceReferenceId == charge.Id &&
                        entry.SourceTransactionType == SourceTransactionType.BanquetCharge));

        FinanceWarnings = new List<ChecklistItem>
        {
            new("Open cashier shifts", openCashierShifts, "Cashier shifts still open for the business date."),
            new("Approved refunds not processed", unprocessedApprovedRefunds, "Approved refunds waiting for processing."),
            new("Pending void approvals", pendingVoidApprovals, "Void requests waiting for approval."),
            new("Pending discount approvals", pendingDiscountApprovals, "Discount requests waiting for approval."),
            new("Unposted folio charges", unpostedFolioItems, "Folio charges not yet posted to accounting. Warning only for MVP."),
            new("Unposted folio payments", unpostedPayments, "Completed payments not yet posted to accounting. Warning only for MVP."),
            new("Unposted POS/banquet transactions", unpostedPosOrBanquet, "POS and banquet source transactions not yet posted to accounting. Warning only for MVP.")
        };
    }

    private async Task<IList<RoomIssue>> LoadRoomIssuesAsync(DateTime businessDate, DateTime nextBusinessDate)
    {
        var issues = new List<RoomIssue>();

        var checkedInReservations = await _context.Reservations
            .Include(reservation => reservation.Room)
            .AsNoTracking()
            .Where(reservation => reservation.Status == ReservationStatus.CheckedIn && reservation.Room != null)
            .ToListAsync();

        foreach (var reservation in checkedInReservations.Where(reservation => reservation.Room!.Status != RoomStatus.Occupied))
        {
            issues.Add(new RoomIssue(
                reservation.Room!.RoomNumber,
                reservation.Room.Status.ToString(),
                $"Checked-in reservation {reservation.ConfirmationNumber} requires room status Occupied."));
        }

        var checkedOutReservations = await _context.Reservations
            .Include(reservation => reservation.Room)
            .AsNoTracking()
            .Where(reservation =>
                reservation.ActualCheckOutDate >= businessDate &&
                reservation.ActualCheckOutDate < nextBusinessDate &&
                reservation.Status == ReservationStatus.CheckedOut &&
                reservation.Room != null)
            .ToListAsync();

        foreach (var reservation in checkedOutReservations.Where(reservation => reservation.Room!.Status == RoomStatus.Occupied))
        {
            issues.Add(new RoomIssue(
                reservation.Room!.RoomNumber,
                reservation.Room.Status.ToString(),
                $"Checked-out reservation {reservation.ConfirmationNumber} cannot leave room Occupied."));
        }

        return issues;
    }

    private async Task LoadHistoryAsync()
    {
        AuditHistory = await _context.NightAudits
            .AsNoTracking()
            .OrderByDescending(audit => audit.BusinessDate)
            .ThenByDescending(audit => audit.CompletedAt)
            .Take(20)
            .ToListAsync();
    }

    private async Task<int> LockChargesAsync(DateTime businessDate)
    {
        var nextBusinessDate = businessDate.AddDays(1);
        var charges = await _context.FolioItems
            .Where(item =>
                item.PostingDate >= businessDate &&
                item.PostingDate < nextBusinessDate &&
                !item.IsLocked)
            .ToListAsync();

        foreach (var charge in charges)
        {
            charge.IsLocked = true;
        }

        return charges.Count;
    }

    private async Task<int> LockPaymentsAsync(DateTime businessDate)
    {
        var nextBusinessDate = businessDate.AddDays(1);
        var payments = await _context.Payments
            .Where(payment =>
                payment.PaymentDate >= businessDate &&
                payment.PaymentDate < nextBusinessDate &&
                !payment.IsLocked)
            .ToListAsync();

        foreach (var payment in payments)
        {
            payment.IsLocked = true;
        }

        return payments.Count;
    }

    private static string BuildAuditSummary(DateTime businessDate, int lockedCharges, int lockedPayments, string? notes)
    {
        var summary = string.Join(Environment.NewLine, new[]
        {
            $"Business date closed: {businessDate:d}",
            $"Locked charges: {lockedCharges}",
            $"Locked payments: {lockedPayments}",
            $"Next business date: {businessDate.AddDays(1):d}"
        });

        return string.IsNullOrWhiteSpace(notes)
            ? summary
            : $"{summary}{Environment.NewLine}Notes: {notes}";
    }

    public record ChecklistItem(string Name, int Count, string Description);

    public record RoomIssue(string RoomNumber, string Status, string Issue);
}
