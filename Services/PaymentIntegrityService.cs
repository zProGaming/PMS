using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public class PaymentIntegrityService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<PaymentIntegritySummary> GetSummaryAsync()
    {
        var rows = await GetIssueRowsAsync(500);
        return new PaymentIntegritySummary(
            rows.Count,
            rows.Count(row => row.Severity is SystemSeverity.Critical or SystemSeverity.High),
            rows.Count(row => row.IssueType == "Missing cashier trace"),
            rows.Count(row => row.IssueType == "Duplicate reference"),
            rows.Count(row => row.IssueType == "Repeat submission"),
            rows.Count(row => row.IssueType == "Credit balance"));
    }

    public async Task<IReadOnlyList<PaymentIntegrityIssueRow>> GetIssueRowsAsync(int take = 300)
    {
        var rows = new List<PaymentIntegrityIssueRow>();
        var limit = Math.Max(50, take);

        await AddPaymentsWithoutCashierTraceAsync(rows, limit);
        await AddDuplicateReferencesAsync(rows, limit);
        await AddRecentRepeatSubmissionsAsync(rows, limit);
        await AddPaymentsAfterFolioCloseAsync(rows, limit);
        await AddNonPositivePaymentsAsync(rows, limit);
        await AddCreditBalanceFoliosAsync(rows, limit);

        return rows
            .GroupBy(row => row.RowKey)
            .Select(group => group.First())
            .OrderByDescending(row => row.Severity)
            .ThenByDescending(row => row.PaymentDate ?? DateTime.MinValue)
            .ThenBy(row => row.IssueType)
            .Take(limit)
            .ToList();
    }

    private async Task AddPaymentsWithoutCashierTraceAsync(List<PaymentIntegrityIssueRow> rows, int take)
    {
        var payments = await BasePaymentQuery()
            .Where(payment => payment.Status == PaymentStatus.Completed && !payment.CashierTransactions.Any())
            .OrderByDescending(payment => payment.PaymentDate)
            .Take(take)
            .ToListAsync();

        foreach (var payment in payments)
        {
            var managementPosted = payment.Notes?.Contains("Management-posted without open cashier shift", StringComparison.OrdinalIgnoreCase) == true;
            rows.Add(ToPaymentRow(
                payment,
                "Missing cashier trace",
                managementPosted ? SystemSeverity.Medium : SystemSeverity.High,
                managementPosted
                    ? "Payment was posted by an authorized manager without an open cashier shift."
                    : "Completed payment has no linked cashier transaction.",
                managementPosted
                    ? "Review management-posted payments during cashier audit."
                    : "Review the receipt and cashier shift history; void and repost if the payment was not authorized."));
        }
    }

    private async Task AddDuplicateReferencesAsync(List<PaymentIntegrityIssueRow> rows, int take)
    {
        var payments = await BasePaymentQuery()
            .Where(payment =>
                payment.Status != PaymentStatus.Voided &&
                payment.Status != PaymentStatus.Failed &&
                payment.ReferenceNumber != null &&
                payment.ReferenceNumber != "")
            .OrderByDescending(payment => payment.PaymentDate)
            .Take(Math.Max(take * 4, 500))
            .ToListAsync();

        foreach (var group in payments
            .GroupBy(payment => new { payment.FolioId, Reference = NormalizeReference(payment.ReferenceNumber) })
            .Where(group => !string.IsNullOrWhiteSpace(group.Key.Reference) && group.Count() > 1))
        {
            foreach (var payment in group)
            {
                rows.Add(ToPaymentRow(
                    payment,
                    "Duplicate reference",
                    SystemSeverity.High,
                    "Multiple payments on the same folio use the same reference number.",
                    "Confirm whether this is a duplicate receipt, processor retry, or valid split settlement."));
            }
        }
    }

    private async Task AddRecentRepeatSubmissionsAsync(List<PaymentIntegrityIssueRow> rows, int take)
    {
        var cutoff = DateTime.Today.AddDays(-14);
        var payments = await BasePaymentQuery()
            .Where(payment =>
                payment.Status != PaymentStatus.Voided &&
                payment.Status != PaymentStatus.Failed &&
                payment.PaymentDate >= cutoff)
            .OrderByDescending(payment => payment.PaymentDate)
            .Take(Math.Max(take * 4, 500))
            .ToListAsync();

        foreach (var group in payments
            .GroupBy(payment => new
            {
                payment.FolioId,
                payment.Amount,
                Method = NormalizePaymentMethod(payment.PaymentMethod),
                Minute = new DateTime(
                    payment.PaymentDate.Year,
                    payment.PaymentDate.Month,
                    payment.PaymentDate.Day,
                    payment.PaymentDate.Hour,
                    payment.PaymentDate.Minute,
                    0)
            })
            .Where(group => group.Count() > 1))
        {
            foreach (var payment in group)
            {
                rows.Add(ToPaymentRow(
                    payment,
                    "Repeat submission",
                    SystemSeverity.High,
                    "Similar payment amount and method were posted to the same folio within the same minute.",
                    "Review the receipts before accepting further settlement on this folio."));
            }
        }
    }

    private async Task AddPaymentsAfterFolioCloseAsync(List<PaymentIntegrityIssueRow> rows, int take)
    {
        var payments = await BasePaymentQuery()
            .Where(payment =>
                payment.Status == PaymentStatus.Completed &&
                payment.Folio != null &&
                payment.Folio.ClosedAtUtc != null)
            .OrderByDescending(payment => payment.PaymentDate)
            .Take(Math.Max(take * 2, 300))
            .ToListAsync();

        foreach (var payment in payments.Where(payment =>
            payment.Folio?.ClosedAtUtc is not null &&
            payment.PaymentDate.ToUniversalTime() > payment.Folio.ClosedAtUtc.Value))
        {
            rows.Add(ToPaymentRow(
                payment,
                "After close",
                SystemSeverity.Medium,
                "Payment was posted after the folio closed.",
                "Confirm whether this was an approved post-checkout collection or should be handled through AR/city ledger controls."));
        }
    }

    private async Task AddNonPositivePaymentsAsync(List<PaymentIntegrityIssueRow> rows, int take)
    {
        var payments = await BasePaymentQuery()
            .Where(payment => payment.Amount <= 0 && payment.Status != PaymentStatus.Voided && payment.Status != PaymentStatus.Failed)
            .OrderByDescending(payment => payment.PaymentDate)
            .Take(take)
            .ToListAsync();

        foreach (var payment in payments)
        {
            rows.Add(ToPaymentRow(
                payment,
                "Invalid amount",
                SystemSeverity.High,
                "Payment amount is zero or negative.",
                "Review and void the payment if it was not posted through an approved adjustment workflow."));
        }
    }

    private async Task AddCreditBalanceFoliosAsync(List<PaymentIntegrityIssueRow> rows, int take)
    {
        var folios = await _context.Folios
            .AsNoTracking()
            .Include(folio => folio.Guest)
            .Include(folio => folio.Reservation).ThenInclude(reservation => reservation!.Room)
            .Select(folio => new
            {
                folio.Id,
                folio.FolioNumber,
                GuestName = folio.Guest == null ? "" : (folio.Guest.FirstName + " " + folio.Guest.LastName).Trim(),
                RoomNumber = folio.Reservation != null && folio.Reservation.Room != null ? folio.Reservation.Room.RoomNumber : null,
                Charges = _context.FolioItems
                    .Where(item => item.FolioId == folio.Id && !item.IsVoided)
                    .Sum(item => (decimal?)item.Amount) ?? 0,
                Payments = _context.Payments
                    .Where(payment => payment.FolioId == folio.Id && payment.Status != PaymentStatus.Voided && payment.Status != PaymentStatus.Failed)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0
            })
            .Where(folio => folio.Charges - folio.Payments < 0)
            .OrderBy(folio => folio.Charges - folio.Payments)
            .Take(take)
            .ToListAsync();

        foreach (var folio in folios)
        {
            rows.Add(new PaymentIntegrityIssueRow(
                $"folio-credit-{folio.Id}",
                "Credit balance",
                SystemSeverity.Medium,
                null,
                folio.Id,
                folio.FolioNumber,
                folio.GuestName,
                folio.RoomNumber,
                null,
                folio.Payments - folio.Charges,
                null,
                null,
                null,
                "Folio has a credit balance after payments.",
                "Review overpayment, refund, AR transfer, or adjustment handling before final reporting."));
        }
    }

    private IQueryable<Payment> BasePaymentQuery()
    {
        return _context.Payments
            .AsNoTracking()
            .Include(payment => payment.Folio).ThenInclude(folio => folio!.Guest)
            .Include(payment => payment.Folio).ThenInclude(folio => folio!.Reservation).ThenInclude(reservation => reservation!.Room)
            .Include(payment => payment.CashierTransactions).ThenInclude(transaction => transaction.CashierShift);
    }

    private static PaymentIntegrityIssueRow ToPaymentRow(
        Payment payment,
        string issueType,
        SystemSeverity severity,
        string description,
        string recommendedAction)
    {
        var folio = payment.Folio;
        var guestName = folio?.Guest is null ? "" : $"{folio.Guest.FirstName} {folio.Guest.LastName}".Trim();
        var shiftNumber = payment.CashierTransactions
            .OrderByDescending(transaction => transaction.TransactionDate)
            .Select(transaction => transaction.CashierShift?.ShiftNumber)
            .FirstOrDefault(number => !string.IsNullOrWhiteSpace(number));

        return new PaymentIntegrityIssueRow(
            $"{issueType}-{payment.Id}",
            issueType,
            severity,
            payment.Id,
            payment.FolioId,
            folio?.FolioNumber ?? "Missing folio",
            guestName,
            folio?.Reservation?.Room?.RoomNumber,
            payment.PaymentDate,
            payment.Amount,
            payment.PaymentMethod,
            payment.ReferenceNumber,
            shiftNumber,
            description,
            recommendedAction);
    }

    private static string NormalizePaymentMethod(string? paymentMethod)
    {
        return string.IsNullOrWhiteSpace(paymentMethod)
            ? string.Empty
            : paymentMethod.Replace("-", string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();
    }

    private static string NormalizeReference(string? referenceNumber)
    {
        return string.IsNullOrWhiteSpace(referenceNumber)
            ? string.Empty
            : referenceNumber.Trim().ToUpperInvariant();
    }
}

public sealed record PaymentIntegritySummary(
    int TotalIssues,
    int HighRiskIssues,
    int MissingCashierTrace,
    int DuplicateReferences,
    int RepeatSubmissions,
    int CreditBalanceFolios);

public sealed record PaymentIntegrityIssueRow(
    string RowKey,
    string IssueType,
    SystemSeverity Severity,
    int? PaymentId,
    int? FolioId,
    string FolioNumber,
    string GuestName,
    string? RoomNumber,
    DateTime? PaymentDate,
    decimal Amount,
    string? PaymentMethod,
    string? ReferenceNumber,
    string? CashierShiftNumber,
    string Description,
    string RecommendedAction);
