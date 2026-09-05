using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Housekeeping;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public sealed record CheckoutRequest(string ReviewToken, bool ManagerOverride, string? OverrideReason, bool CreditsAcknowledged);
public sealed record CheckoutResult(bool Found, bool Completed, IReadOnlyList<string> Errors);

public sealed record CheckoutReview(Reservation Reservation)
{
    // AR transfers have a separate collection workflow. Never net guest credits
    // against debt on another folio, or silently close a credit-bearing folio.
    public IReadOnlyList<Folio> RelevantFolios => Reservation.Folios
        .Where(f => f.Status != FolioStatus.Voided && f.Status != FolioStatus.Transferred)
        .OrderBy(f => f.Id).ToList();
    public decimal AmountDue => RelevantFolios.Sum(f => Math.Max(0, f.Balance));
    public decimal GuestCredit => RelevantFolios.Sum(f => Math.Max(0, -f.Balance));
    public string Token => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        Reservation.Id, Reservation.Status, Reservation.RoomId, Reservation.DepartureDate,
        Reservation.ActualCheckInDate, Reservation.ActualCheckOutDate,
        Folios = Reservation.Folios.OrderBy(f => f.Id).Select(f => new
        {
            f.Id, f.Status,
            Items = f.Items.OrderBy(i => i.Id).Select(i => new { i.Id, i.Amount, i.IsVoided }),
            Payments = f.Payments.OrderBy(p => p.Id).Select(p => new { p.Id, p.Amount, p.Status })
        })
    })));

    public static bool CanOverride(ClaimsPrincipal user) => new[]
    {
        PmsRoles.SystemAdmin, PmsRoles.GeneralManager, PmsRoles.FrontOfficeManager, PmsRoles.FinanceManager
    }.Any(user.IsInRole);

    public IReadOnlyList<string> Validate(CheckoutRequest request, ClaimsPrincipal user)
    {
        var errors = new List<string>();
        if (Reservation.Status != ReservationStatus.CheckedIn)
            errors.Add("Only checked-in reservations can be checked out.");
        if (Reservation.RoomId is null || Reservation.Room is null)
            errors.Add("Assign a room before checking out.");
        if (Reservation.Folios.Count == 0)
            errors.Add("No folio exists for this stay. Ask Front Office to resolve the missing ledger before checkout.");
        if (RelevantFolios.Any(f => f.Status == FolioStatus.Closed && f.Balance != 0))
            errors.Add("A closed folio has an unsettled balance. Finance must correct its ledger status before checkout.");
        if (!string.Equals(request.ReviewToken, Token, StringComparison.Ordinal))
            errors.Add("This stay or its folios changed after you opened checkout. Review the refreshed amounts and confirm again.");
        if (AmountDue > 0)
        {
            if (!request.ManagerOverride || !CanOverride(user))
                errors.Add("Collect the amount due on each folio, or ask an authorized manager to approve checkout with a collection plan.");
            else if ((request.OverrideReason?.Trim().Length ?? 0) is < 10 or > 500)
                errors.Add("Enter an override reason and collection follow-up (10–500 characters).");
        }
        if (GuestCredit > 0 && !request.CreditsAcknowledged)
            errors.Add("Acknowledge the guest credit and refer it to Finance for refund review. Checkout does not issue a refund.");
        return errors;
    }
}

public sealed class CheckoutService(ApplicationDbContext context)
{
    public Task<Reservation?> LoadAsync(int id) => Query().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    private IQueryable<Reservation> Query() => context.Reservations
        .Include(r => r.Room).Include(r => r.Guest)
        .Include(r => r.Folios).ThenInclude(f => f.Items)
        .Include(r => r.Folios).ThenInclude(f => f.Payments).AsSplitQuery();

    public async Task<CheckoutResult> CompleteAsync(int id, CheckoutRequest request, ClaimsPrincipal user)
    {
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // A retry must reload database state, not reuse a failed attempt's entities.
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await ReservationLedgerLock.AcquireAsync(context, id);
            var reservation = await Query().FirstOrDefaultAsync(r => r.Id == id);
            if (reservation is null) return new CheckoutResult(false, false, []);
            // Safe replay, including retries after an uncertain commit acknowledgement.
            if (reservation.Status == ReservationStatus.CheckedOut)
                return new CheckoutResult(true, true, []);

            var review = new CheckoutReview(reservation);
            var errors = review.Validate(request, user);
            if (errors.Count != 0) return new CheckoutResult(true, false, errors);

            var amountDue = review.AmountDue;
            var guestCredit = review.GuestCredit;
            reservation.Status = ReservationStatus.CheckedOut;
            reservation.ActualCheckOutDate = DateTime.Now;
            reservation.ManagerOverrideRequested = amountDue > 0 && request.ManagerOverride;
            reservation.Room!.Status = RoomStatus.Dirty;
            foreach (var folio in review.RelevantFolios.Where(f => f.Status == FolioStatus.Open && f.Balance == 0))
            {
                folio.Status = FolioStatus.Closed;
                folio.ClosedAtUtc = DateTime.UtcNow;
            }

            var turnoverNote = $"Automatically created after checkout (reservation {reservation.Id}).";
            if (!await context.HousekeepingTasks.AnyAsync(t => t.RoomId == reservation.RoomId && t.Notes == turnoverNote))
                context.HousekeepingTasks.Add(new HousekeepingTask
                {
                    RoomId = reservation.RoomId!.Value, Priority = HousekeepingTaskPriority.High,
                    TaskStatus = HousekeepingTaskStatus.Open, AssignedTo = "Housekeeping Queue", Notes = turnoverNote
                });

            // The decision is atomic with the room, folios, automatic audit and task.
            context.AuditLogs.Add(new AuditLog
            {
                Module = "Front Office", EntityName = "CheckoutDecision", EntityId = id.ToString(),
                Action = amountDue > 0 ? AuditActionType.Approve : AuditActionType.Update,
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier), UserName = user.Identity?.Name,
                CreatedAt = DateTime.Now,
                NewValues = JsonSerializer.Serialize(new
                {
                    ReservationId = id, OccurredAtUtc = DateTime.UtcNow, AmountDue = amountDue, GuestCredit = guestCredit,
                    ManagerOverride = reservation.ManagerOverrideRequested,
                    OverrideReason = amountDue > 0 ? request.OverrideReason?.Trim() : null,
                    CreditsAcknowledged = guestCredit > 0 && request.CreditsAcknowledged,
                    FolioIds = review.RelevantFolios.Select(f => f.Id).ToArray()
                })
            });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new CheckoutResult(true, true, []);
        });
    }
}

// Posting workflows take this same SQL Server transaction-owned lock before
// re-reading balances, serializing payment/charge settlement against checkout.
public static class ReservationLedgerLock
{
    public static async Task AcquireAsync(ApplicationDbContext context, int reservationId)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A ledger lock requires an active transaction.");
        await context.Reservations.FromSqlInterpolated(
            $"SELECT * FROM [Reservations] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {reservationId}")
            .AsNoTracking().ToListAsync();
    }
}
