using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Services;

public class CashierControlService(ApplicationDbContext context, FinanceService finance)
{
    public Task<IList<string>> UpdateAsync(int id, ClaimsPrincipal user, decimal amount, bool close, string? receivedBy = null, string? notes = null) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync<IList<string>>(async () =>
        {
            if (string.IsNullOrWhiteSpace(user.Identity?.Name) || !Authorization.PmsRoles.Finance.Any(user.IsInRole)) return ["Finance access is required."];
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var shift = await CashierShiftLock.AcquireAsync(context, id);
            if (shift is null) return ["Cashier shift was not found."];
            if (!FinanceAdjustmentService.CanApprove(user) && !FinanceAdjustmentService.SameActor(shift.OpenedBy, user.Identity?.Name))
                return ["Only the shift owner or a finance manager can change this shift."];
            if (shift.Status != CashierShiftStatus.Open) return ["This shift is no longer open. Refresh before continuing."];
            if (amount < 0 || (!close && amount == 0)) return ["Enter a valid, non-negative cash count or a positive cash drop."];
            if (decimal.Round(amount, 2) != amount) return ["Cash amounts must have no more than two decimal places."];
            await context.Entry(shift).Collection(s => s.Transactions).LoadAsync();
            await context.Entry(shift).Collection(s => s.CashDrops).LoadAsync();
            var expected = finance.CalculateExpectedCash(shift);
            if (close)
            {
                shift.ClosingCashCount = amount; shift.ExpectedCashAmount = expected; shift.CashOverShort = amount - expected;
                shift.ClosedBy = user.Identity?.Name; shift.ClosedAt = DateTime.Now; shift.Status = CashierShiftStatus.Closed;
            }
            else
            {
                if (amount > expected) return ["The cash drop exceeds the expected cash remaining in this shift."];
                if (string.IsNullOrWhiteSpace(receivedBy)) return ["Enter the name of the person receiving the cash drop."];
                context.CashDrops.Add(new CashDrop { CashierShiftId = id, Amount = amount, DroppedBy = user.Identity!.Name!, ReceivedBy = receivedBy.Trim(), Notes = notes, DropDate = DateTime.Now });
                context.CashierTransactions.Add(new CashierTransaction
                {
                    CashierShiftId = id, Amount = amount, TransactionType = CashierTransactionType.CashDrop,
                    PaymentMethod = FinancePaymentMethod.Cash, CreatedBy = user.Identity!.Name!, Notes = notes, TransactionDate = DateTime.Now
                });
            }
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return [];
        });
}
