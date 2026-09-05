using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

// Financial decisions share the reservation lock with payment posting and checkout.
// Lock order: reservation, request, cashier shift. No external payout is initiated here.
public class FinanceAdjustmentService(ApplicationDbContext context)
{
    public static bool CanApprove(ClaimsPrincipal user) =>
        user.IsInRole(PmsRoles.SystemAdmin) || user.IsInRole(PmsRoles.GeneralManager) || user.IsInRole(PmsRoles.FinanceManager);

    public static bool SameActor(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool CanWork(ClaimsPrincipal user) =>
        !string.IsNullOrWhiteSpace(user.Identity?.Name) && (CanApprove(user) || user.IsInRole(PmsRoles.Cashier));

    public async Task<IList<string>> CreateRefundAsync(RefundTransaction input, ClaimsPrincipal user)
    {
        if (!CanWork(user)) return ["Finance access is required."];
        var payment = await context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == input.PaymentId);
        if (payment is null || payment.Amount <= 0 || payment.Status != PaymentStatus.Completed)
            return ["Select a completed source receipt. Legacy refunded receipts require finance reconciliation."];
        if (input.FolioId.HasValue && input.FolioId != payment.FolioId) return ["The source receipt does not belong to the selected folio."];
        if (input.Amount <= 0 || input.Amount > payment.Amount) return ["Refund amount must be positive and cannot exceed the source receipt."];
        if (decimal.Round(input.Amount, 2) != input.Amount) return ["Refund amount must have no more than two decimal places."];
        var refunded = await context.RefundTransactions.Where(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Processed).SumAsync(r => r.Amount);
        if (input.Amount > payment.Amount - refunded) return ["Refund amount exceeds the remaining refundable amount on this receipt."];
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < 10 || input.Reason.Length > 500)
            return ["Enter a refund reason of 10 to 500 characters."];
        if (!Enum.IsDefined(input.RefundMethod)) return ["Select a valid refund method."];
        // Never bind approval, processing, navigation objects, or identity fields from the browser.
        var refund = new RefundTransaction
        {
            RefundNumber = $"REF-{DateTime.Today:yyyyMMdd}-{Guid.NewGuid():N}", RefundDate = DateTime.Now,
            PaymentId = payment.Id, FolioId = payment.FolioId, Amount = input.Amount,
            RefundMethod = input.RefundMethod, Reason = input.Reason.Trim(), RequestedBy = user.Identity!.Name!,
            Status = RefundStatus.Requested
        };
        context.RefundTransactions.Add(refund);
        await context.SaveChangesAsync();
        return [];
    }

    public Task<IList<string>> DecideRefundAsync(int id, string action, ClaimsPrincipal user) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync<IList<string>>(async () =>
        {
            if (!CanWork(user)) return ["Finance access is required."];
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var snapshot = await context.RefundTransactions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (snapshot is null) return ["Refund request was not found."];
            var sourceFolioId = await context.Payments.Where(p => p.Id == snapshot.PaymentId).Select(p => (int?)p.FolioId).FirstOrDefaultAsync();
            await LockFolioAsync(sourceFolioId ?? snapshot.FolioId);
            var refund = (await context.RefundTransactions.FromSqlInterpolated(
                $"SELECT * FROM [RefundTransactions] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").ToListAsync()).Single();
            var actor = user.Identity!.Name!;
            var before = refund.Status;
            if (action == "Process" && before == RefundStatus.Processed) return []; // Retry without duplicate ledger entries.
            if (action is "Approve" or "Reject")
            {
                if (!CanApprove(user)) return ["Finance manager approval is required."];
                if (SameActor(refund.RequestedBy, actor)) return ["A different manager must review your own refund request."];
                if (before is not (RefundStatus.Requested or RefundStatus.ForApproval)) return ["This request is no longer awaiting approval. Refresh the queue."];
                refund.Status = action == "Approve" ? RefundStatus.Approved : RefundStatus.Rejected;
                if (action == "Approve") { refund.ApprovedBy = actor; refund.ApprovedAt = DateTime.Now; }
            }
            else if (action == "Cancel")
            {
                if (!CanApprove(user) && !SameActor(refund.RequestedBy, actor)) return ["Only the requester or a finance manager can cancel this request."];
                if (before is not (RefundStatus.Requested or RefundStatus.ForApproval or RefundStatus.Approved)) return ["Only unprocessed, active requests can be cancelled."];
                refund.Status = RefundStatus.Cancelled;
            }
            else if (action == "Process")
            {
                if (before != RefundStatus.Approved) return ["Only approved refunds can be processed."];
                if (string.IsNullOrWhiteSpace(refund.ApprovedBy) || SameActor(refund.RequestedBy, refund.ApprovedBy) || SameActor(refund.ApprovedBy, actor))
                    return ["Processing requires an independent approval and an operator other than the approver."];
                var payment = await context.Payments.FirstOrDefaultAsync(p => p.Id == refund.PaymentId);
                if (payment is null || payment.Amount <= 0 || payment.Status != PaymentStatus.Completed)
                    return ["A completed source receipt is required. Do not reprocess a voided or legacy refunded receipt."];
                if (refund.FolioId.HasValue && refund.FolioId != payment.FolioId) return ["The source receipt does not belong to this folio."];
                if (refund.Amount <= 0 || !Enum.IsDefined(refund.RefundMethod) || string.IsNullOrWhiteSpace(refund.Reason)) return ["The refund amount, method, and reason must be valid."];
                var alreadyRefunded = await context.RefundTransactions.Where(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Processed).SumAsync(r => r.Amount);
                if (alreadyRefunded + refund.Amount > payment.Amount) return ["Refund amount exceeds the remaining refundable amount."];
                var folio = await context.Folios.Include(f => f.Items).Include(f => f.Payments).FirstAsync(f => f.Id == payment.FolioId);
                if (folio.Status is FolioStatus.Voided or FolioStatus.Transferred) return ["Transferred or voided folios require finance reconciliation before a refund."];
                var shift = await CashierShiftLock.OpenForUserAsync(context, actor);
                if (shift is null) return ["Open your cashier shift before processing a refund."];
                if (refund.RefundMethod == FinancePaymentMethod.Cash)
                {
                    await context.Entry(shift).Collection(s => s.Transactions).LoadAsync();
                    await context.Entry(shift).Collection(s => s.CashDrops).LoadAsync();
                    if (new FinanceService(context).CalculateExpectedCash(shift) < refund.Amount)
                        return ["The cash refund exceeds the expected cash available in your shift. Finance must arrange the payout funds first."];
                }
                var balanceAfterRefund = folio.Balance + refund.Amount;
                var reversal = new Payment
                {
                    FolioId = payment.FolioId, Amount = -refund.Amount, PaymentMethod = $"Refund - {refund.RefundMethod}",
                    PaymentDate = DateTime.Now, ReferenceNumber = refund.RefundNumber, Notes = refund.Reason, Status = PaymentStatus.Completed
                };
                context.Payments.Add(reversal);
                context.CashierTransactions.Add(new CashierTransaction
                {
                    CashierShiftId = shift.Id, FolioId = payment.FolioId, Payment = reversal,
                    TransactionType = CashierTransactionType.Refund, Amount = refund.Amount, PaymentMethod = refund.RefundMethod,
                    TransactionDate = DateTime.Now, ReferenceNumber = refund.RefundNumber, Notes = refund.Reason, CreatedBy = actor
                });
                // The original receipt stays Completed. The negative entry offsets it exactly once.
                // Marking the original Refunded as well would incorrectly reverse it a second time.
                if (folio.Status == FolioStatus.Closed && balanceAfterRefund != 0)
                { folio.Status = FolioStatus.Open; folio.ClosedAtUtc = null; }
                refund.FolioId = payment.FolioId;
                refund.Status = RefundStatus.Processed; refund.ProcessedBy = actor; refund.ProcessedAt = DateTime.Now;
            }
            else return ["Unsupported refund decision."];
            Audit("RefundDecision", id, action, user, new { Before = before, After = refund.Status, refund.Amount, refund.PaymentId, refund.FolioId, refund.Reason, refund.RequestedBy, refund.ApprovedBy });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return [];
        });

    public async Task<IList<string>> CreateVoidAsync(VoidRequest input, ClaimsPrincipal user)
    {
        if (!CanWork(user)) return ["Finance access is required."];
        if (input.ReferenceType is not ("FolioItem" or "Payment")) return ["This workflow supports folio charges and receipts. POS and issued documents require their own reconciliation workflow."];
        if (input.ReferenceId <= 0) return ["Enter a valid source reference ID."];
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < 10 || input.Reason.Length > 500) return ["Enter a void reason of 10 to 500 characters."];
        var folioId = await VoidFolioIdAsync(input.ReferenceType, input.ReferenceId);
        if (folioId is null) return ["The source transaction was not found."];
        context.VoidRequests.Add(new VoidRequest
        {
            ReferenceType = input.ReferenceType, ReferenceId = input.ReferenceId, Reason = input.Reason.Trim(),
            RequestedBy = user.Identity!.Name!, RequestedAt = DateTime.Now, Status = ApprovalStatus.Pending
        });
        await context.SaveChangesAsync();
        return [];
    }

    public Task<IList<string>> DecideVoidAsync(int id, string action, ClaimsPrincipal user) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync<IList<string>>(async () =>
        {
            if (!CanWork(user)) return ["Finance access is required."];
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var snapshot = await context.VoidRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (snapshot is null) return ["Void request was not found."];
            await LockFolioAsync(await VoidFolioIdAsync(snapshot.ReferenceType, snapshot.ReferenceId));
            var request = (await context.VoidRequests.FromSqlInterpolated(
                $"SELECT * FROM [VoidRequests] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").ToListAsync()).Single();
            var actor = user.Identity!.Name!;
            var before = request.Status;
            if (action == "Process" && before == ApprovalStatus.Processed) return [];
            if (action is "Approve" or "Reject")
            {
                if (!CanApprove(user)) return ["Finance manager approval is required."];
                if (SameActor(request.RequestedBy, actor)) return ["A different manager must review your own void request."];
                if (before != ApprovalStatus.Pending) return ["This request is no longer awaiting approval. Refresh the queue."];
                request.Status = action == "Approve" ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
                if (action == "Approve") { request.ApprovedBy = actor; request.ApprovedAt = DateTime.Now; }
                else { request.RejectedBy = actor; request.RejectedAt = DateTime.Now; }
            }
            else if (action == "Process")
            {
                if (before != ApprovalStatus.Approved) return ["Only approved void requests can be processed."];
                if (string.IsNullOrWhiteSpace(request.ApprovedBy) || SameActor(request.RequestedBy, request.ApprovedBy) || SameActor(request.ApprovedBy, actor))
                    return ["Processing requires an independent approval and an operator other than the approver."];
                if (string.IsNullOrWhiteSpace(request.Reason)) return ["A documented void reason is required."];
                var folioId = await VoidFolioIdAsync(request.ReferenceType, request.ReferenceId);
                if (folioId is null) return ["Only folio charges and receipts can be processed here. Other source types require reconciliation in their own module."];
                var folio = await context.Folios.FirstAsync(f => f.Id == folioId);
                if (folio.Status != FolioStatus.Open) return ["Only an open folio can be corrected by a void. Use the refund or finance reconciliation workflow for settled records."];
                if (request.ReferenceType == "Payment")
                {
                    var payment = await context.Payments.FirstAsync(p => p.Id == request.ReferenceId);
                    if (payment.IsLocked || payment.Status != PaymentStatus.Completed || payment.Amount <= 0) return ["Only an unlocked, completed original receipt can be voided."];
                    if (await context.RefundTransactions.AnyAsync(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Processed)) return ["A receipt with processed refunds cannot also be voided."];
                    var traces = await context.CashierTransactions.Where(t => t.PaymentId == payment.Id && !t.IsVoided).OrderBy(t => t.CashierShiftId).ToListAsync();
                    if (traces.Count == 0 || traces.Sum(t => t.Amount) != payment.Amount ||
                        traces.Any(t => t.CashierShiftId == null || t.FolioId != payment.FolioId || t.TransactionType != CashierTransactionType.Payment))
                        return ["This receipt has no consistent cashier trace. Finance reconciliation is required."];
                    foreach (var shiftId in traces.Select(t => t.CashierShiftId!.Value).Distinct())
                    {
                        var shift = await CashierShiftLock.AcquireAsync(context, shiftId);
                        if (shift?.Status != CashierShiftStatus.Open) return ["A receipt from a closed or audited shift cannot be voided. Use an approved refund in a new shift."];
                        if (!CanApprove(user) && !SameActor(shift.OpenedBy, actor)) return ["Only the shift owner or a finance manager can void this receipt."];
                    }
                    payment.Status = PaymentStatus.Voided;
                    foreach (var trace in traces) trace.IsVoided = true;
                }
                else
                {
                    var item = await context.FolioItems.FirstAsync(i => i.Id == request.ReferenceId);
                    if (item.IsVoided || item.IsLocked) return ["This charge is already voided or locked. Finance reconciliation is required."];
                    item.IsVoided = true;
                }
                request.Status = ApprovalStatus.Processed;
                request.Notes = $"{request.Notes} Processed by {actor} at {DateTime.UtcNow:O}.".Trim();
            }
            else return ["Unsupported void decision."];
            Audit("VoidDecision", id, action, user, new { Before = before, After = request.Status, request.ReferenceType, request.ReferenceId, request.Reason, request.RequestedBy, request.ApprovedBy });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return [];
        });

    private Task<int?> VoidFolioIdAsync(string type, int id) => type switch
    {
        "Payment" => context.Payments.Where(p => p.Id == id).Select(p => (int?)p.FolioId).FirstOrDefaultAsync(),
        "FolioItem" => context.FolioItems.Where(i => i.Id == id).Select(i => (int?)i.FolioId).FirstOrDefaultAsync(),
        _ => Task.FromResult<int?>(null)
    };

    private async Task LockFolioAsync(int? folioId)
    {
        var reservationId = await context.Folios.Where(f => f.Id == folioId).Select(f => (int?)f.ReservationId).FirstOrDefaultAsync();
        if (reservationId.HasValue) await ReservationLedgerLock.AcquireAsync(context, reservationId.Value);
    }

    private void Audit(string entity, int id, string action, ClaimsPrincipal user, object decision) => context.AuditLogs.Add(new AuditLog
    {
        EntityName = entity, EntityId = id.ToString(), Module = "Finance", Action = AuditActionType.Update,
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier), UserName = user.Identity?.Name,
        NewValues = JsonSerializer.Serialize(new { Action = action, OccurredAtUtc = DateTime.UtcNow, Decision = decision })
    });
}

public static class CashierShiftLock
{
    public static async Task<CashierShift?> AcquireAsync(ApplicationDbContext context, int id)
    {
        if (context.Database.CurrentTransaction is null) throw new InvalidOperationException("A cashier lock requires an active transaction.");
        return (await context.CashierShifts.FromSqlInterpolated(
            $"SELECT * FROM [CashierShifts] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").ToListAsync()).SingleOrDefault();
    }

    public static async Task<CashierShift?> OpenForUserAsync(ApplicationDbContext context, string actor)
    {
        var id = await context.CashierShifts.AsNoTracking().Where(s => s.OpenedBy == actor && s.Status == CashierShiftStatus.Open).OrderBy(s => s.Id).Select(s => (int?)s.Id).FirstOrDefaultAsync();
        var shift = id.HasValue ? await AcquireAsync(context, id.Value) : null;
        return shift?.Status == CashierShiftStatus.Open ? shift : null;
    }
}
