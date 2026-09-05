using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;
using Xunit;

namespace Vantage.PMS.Tests;

[Collection("Checkout SQL")]
public class FinanceAdjustmentTests(CheckoutDatabase database)
{
    internal static ClaimsPrincipal Actor(string role, string name) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, name), new Claim(ClaimTypes.Name, name), new Claim(ClaimTypes.Role, role)
    }, "Identity.Application"));

    private async Task<(int Folio, int Payment, int Refund, int Shift, string Operator)> SeedAsync(decimal refundAmount = 1000)
    {
        var stayId = await database.SeedStayAsync(0);
        await using var context = database.Open();
        var folio = await context.Folios.Include(f => f.Payments).SingleAsync(f => f.ReservationId == stayId);
        var payment = folio.Payments.Single();
        var actor = $"cashier-{Guid.NewGuid():N}";
        var shift = new CashierShift { ShiftNumber = actor, OpenedBy = actor, OpeningCashFloat = 2000 };
        var refund = new RefundTransaction
        {
            PaymentId = payment.Id, FolioId = folio.Id, RefundNumber = $"QA-{Guid.NewGuid():N}", Amount = refundAmount,
            Reason = "Approved training refund.", RequestedBy = actor, ApprovedBy = "independent.manager", ApprovedAt = DateTime.Now, Status = RefundStatus.Approved
        };
        context.CashierShifts.Add(shift); context.RefundTransactions.Add(refund);
        context.CashierTransactions.Add(new CashierTransaction { CashierShift = shift, PaymentId = payment.Id, FolioId = folio.Id, Amount = payment.Amount, CreatedBy = actor, TransactionType = CashierTransactionType.Payment });
        await context.SaveChangesAsync();
        return (folio.Id, payment.Id, refund.Id, shift.Id, actor);
    }

    [Theory] [InlineData(1000, 1000)] [InlineData(250, 250)]
    public async Task RefundOffsetsOriginalExactlyOnceAndTracesNegativeReceipt(decimal amount, decimal expectedBalance)
    {
        var seed = await SeedAsync(amount);
        await using var context = database.Open();
        var service = new FinanceAdjustmentService(context);
        var actor = Actor(PmsRoles.Cashier, seed.Operator);
        Assert.Empty(await service.DecideRefundAsync(seed.Refund, "Process", actor));
        Assert.Empty(await service.DecideRefundAsync(seed.Refund, "Process", actor));
        await using var verify = database.Open();
        var folio = await verify.Folios.Include(f => f.Items).Include(f => f.Payments).SingleAsync(f => f.Id == seed.Folio);
        Assert.Equal(expectedBalance, folio.Balance);
        Assert.Equal(PaymentStatus.Completed, folio.Payments.Single(p => p.Id == seed.Payment).Status);
        var reversal = Assert.Single(folio.Payments, p => p.Amount < 0);
        var trace = await verify.CashierTransactions.SingleAsync(t => t.PaymentId == reversal.Id);
        Assert.Equal(CashierTransactionType.Refund, trace.TransactionType);
        Assert.Equal(amount, trace.Amount);
        var shift = await verify.CashierShifts.Include(s => s.Transactions).Include(s => s.CashDrops).SingleAsync(s => s.Id == seed.Shift);
        Assert.Equal(3000 - amount, new FinanceService(verify).CalculateExpectedCash(shift));
        Assert.Single(await verify.AuditLogs.Where(a => a.EntityName == "RefundDecision" && a.EntityId == seed.Refund.ToString()).ToListAsync());
        Assert.DoesNotContain(await new PaymentIntegrityService(verify).GetIssueRowsAsync(1000), r => r.PaymentId == reversal.Id && r.IssueType == "Invalid amount");
    }

    [Fact] public async Task ConcurrentRefundsCannotExceedSourceReceipt()
    {
        var seed = await SeedAsync(700);
        int otherId;
        await using (var context = database.Open())
        {
            var other = new RefundTransaction { PaymentId = seed.Payment, FolioId = seed.Folio, Amount = 700, RefundNumber = "QA-OTHER-" + seed.Payment, Reason = "Second training request.", Status = RefundStatus.Approved, RequestedBy = seed.Operator, ApprovedBy = "independent.manager" };
            context.RefundTransactions.Add(other); await context.SaveChangesAsync(); otherId = other.Id;
        }
        async Task<IList<string>> Process(int id)
        {
            await using var context = database.Open();
            return await new FinanceAdjustmentService(context).DecideRefundAsync(id, "Process", Actor(PmsRoles.Cashier, seed.Operator));
        }
        var results = await Task.WhenAll(Process(seed.Refund), Process(otherId));
        Assert.Single(results, r => r.Count == 0);
        Assert.Contains(results.SelectMany(r => r), e => e.Contains("remaining refundable"));
        await using var verify = database.Open();
        Assert.Equal(700, await verify.RefundTransactions.Where(r => r.PaymentId == seed.Payment && r.Status == RefundStatus.Processed).SumAsync(r => r.Amount));
    }

    [Theory] [InlineData("SelfApproval")] [InlineData("ApproverProcesses")] [InlineData("ClosedShift")] [InlineData("WrongFolio")] [InlineData("LegacyReceipt")] [InlineData("WrongRole")]
    public async Task InvalidRefundControlsDoNotWriteLedger(string scenario)
    {
        var seed = await SeedAsync();
        await using var context = database.Open();
        var request = await context.RefundTransactions.FindAsync(seed.Refund);
        if (scenario == "SelfApproval") request!.ApprovedBy = seed.Operator;
        if (scenario == "WrongFolio") request!.FolioId = null; // Set a real but mismatched folio below.
        if (scenario == "ClosedShift") (await context.CashierShifts.FindAsync(seed.Shift))!.Status = CashierShiftStatus.Closed;
        if (scenario == "LegacyReceipt") (await context.Payments.FindAsync(seed.Payment))!.Status = PaymentStatus.Refunded;
        if (scenario == "WrongFolio")
        {
            var otherStay = await database.SeedStayAsync(0);
            request!.FolioId = await context.Folios.Where(f => f.ReservationId == otherStay).Select(f => f.Id).SingleAsync();
        }
        await context.SaveChangesAsync();
        var actor = Actor(scenario == "WrongRole" ? PmsRoles.Housekeeper : PmsRoles.Cashier, scenario == "ApproverProcesses" ? "independent.manager" : seed.Operator);
        Assert.NotEmpty(await new FinanceAdjustmentService(context).DecideRefundAsync(seed.Refund, "Process", actor));
        await using var verify = database.Open();
        Assert.False(await verify.Payments.AnyAsync(p => p.FolioId == seed.Folio && p.Amount < 0));
        Assert.Equal(RefundStatus.Approved, (await verify.RefundTransactions.FindAsync(seed.Refund))!.Status);
    }

    [Fact] public async Task CreateIgnoresForgedIdentitiesAndRequiresIndependentApproval()
    {
        var seed = await SeedAsync(100);
        await using var context = database.Open();
        var service = new FinanceAdjustmentService(context);
        var maker = Actor(PmsRoles.FinanceManager, "requesting.manager");
        var malicious = new RefundTransaction { Id = seed.Refund, PaymentId = seed.Payment, Amount = 100, Reason = "Training refund reason.", RequestedBy = "forged.user", ApprovedBy = "forged.approver", ProcessedBy = "forged.operator", Status = RefundStatus.Processed };
        Assert.Empty(await service.CreateRefundAsync(malicious, maker));
        var created = await context.RefundTransactions.SingleAsync(r => r.PaymentId == seed.Payment && r.Id != seed.Refund);
        Assert.Equal("requesting.manager", created.RequestedBy); Assert.Null(created.ApprovedBy); Assert.Null(created.ProcessedBy); Assert.Equal(RefundStatus.Requested, created.Status);
        Assert.NotEmpty(await service.DecideRefundAsync(created.Id, "Approve", maker));
        Assert.Empty(await service.DecideRefundAsync(created.Id, "Approve", Actor(PmsRoles.FinanceManager, "independent.manager")));
        Assert.NotEmpty(await service.DecideRefundAsync(created.Id, "Cancel", Actor(PmsRoles.Cashier, "other.cashier")));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task PaymentVoidIsTerminalAndCannotRewriteClosedShift(bool closed)
    {
        var seed = await SeedAsync();
        await using var context = database.Open();
        if (closed) (await context.CashierShifts.FindAsync(seed.Shift))!.Status = CashierShiftStatus.Closed;
        var request = new VoidRequest { ReferenceType = "Payment", ReferenceId = seed.Payment, Reason = "Duplicate training receipt.", RequestedBy = seed.Operator, ApprovedBy = "independent.manager", Status = ApprovalStatus.Approved };
        context.VoidRequests.Add(request); await context.SaveChangesAsync();
        var service = new FinanceAdjustmentService(context);
        var errors = await service.DecideVoidAsync(request.Id, "Process", Actor(PmsRoles.Cashier, seed.Operator));
        Assert.Equal(closed, errors.Count > 0);
        if (!closed) Assert.Empty(await service.DecideVoidAsync(request.Id, "Process", Actor(PmsRoles.Cashier, seed.Operator)));
        await using var verify = database.Open();
        Assert.Equal(closed ? ApprovalStatus.Approved : ApprovalStatus.Processed, (await verify.VoidRequests.FindAsync(request.Id))!.Status);
        Assert.Equal(closed ? PaymentStatus.Completed : PaymentStatus.Voided, (await verify.Payments.FindAsync(seed.Payment))!.Status);
        Assert.Equal(!closed, (await verify.CashierTransactions.SingleAsync(t => t.PaymentId == seed.Payment)).IsVoided);
    }

    [Fact] public async Task CashierCannotCloseAnotherOperatorsShift()
    {
        var seed = await SeedAsync();
        await using var context = database.Open();
        var controls = new CashierControlService(context, new FinanceService(context));
        Assert.NotEmpty(await controls.UpdateAsync(seed.Shift, Actor(PmsRoles.Cashier, "other.cashier"), 3000, true));
        Assert.NotEmpty(await controls.UpdateAsync(seed.Shift, Actor(PmsRoles.Cashier, seed.Operator), -1, true));
        Assert.Empty(await controls.UpdateAsync(seed.Shift, Actor(PmsRoles.Cashier, seed.Operator), 3000, true));
        Assert.NotEmpty(await new FinanceAdjustmentService(context).DecideRefundAsync(seed.Refund, "Process", Actor(PmsRoles.Cashier, seed.Operator)));
    }

    [Fact] public async Task ConcurrentRetriesProduceOneRefundAndReopenTheSettledFolio()
    {
        var seed = await SeedAsync();
        await using (var setup = database.Open())
        {
            var folio = (await setup.Folios.FindAsync(seed.Folio))!;
            folio.Status = FolioStatus.Closed; folio.ClosedAtUtc = DateTime.UtcNow;
            await setup.SaveChangesAsync();
        }
        async Task<IList<string>> Process()
        {
            await using var context = database.Open();
            return await new FinanceAdjustmentService(context).DecideRefundAsync(seed.Refund, "Process", Actor(PmsRoles.Cashier, seed.Operator));
        }
        foreach (var result in await Task.WhenAll(Process(), Process())) Assert.Empty(result);
        await using var verify = database.Open();
        Assert.Equal(1, await verify.Payments.CountAsync(p => p.FolioId == seed.Folio && p.Amount < 0));
        Assert.Equal(FolioStatus.Open, (await verify.Folios.FindAsync(seed.Folio))!.Status);
    }

    [Fact] public async Task ClosingShiftSerializesAgainstPaymentPosting()
    {
        var seed = await SeedAsync();
        await using (var setup = database.Open())
        {
            setup.FolioItems.Add(new FolioItem { FolioId = seed.Folio, Amount = 100, Description = "Training charge", ChargeCode = "QA" });
            await setup.SaveChangesAsync();
        }
        async Task<IList<string>> Pay()
        {
            await using var context = database.Open();
            return await new FinanceService(context).PostFolioPaymentAsync(new Payment { FolioId = seed.Folio, Amount = 100, PaymentMethod = "Cash", PaymentDate = DateTime.Now, ReferenceNumber = "QA-CLOSE-RACE" }, seed.Operator, false);
        }
        async Task<IList<string>> Close()
        {
            await using var context = database.Open();
            return await new CashierControlService(context, new FinanceService(context)).UpdateAsync(seed.Shift, Actor(PmsRoles.Cashier, seed.Operator), 3000, true);
        }
        var results = await Task.WhenAll(Pay(), Close());
        Assert.Empty(results[1]);
        await using var verify = database.Open();
        var shift = await verify.CashierShifts.Include(s => s.Transactions).Include(s => s.CashDrops).SingleAsync(s => s.Id == seed.Shift);
        Assert.Equal(CashierShiftStatus.Closed, shift.Status);
        Assert.Equal(3000 + (results[0].Count == 0 ? 100 : 0), shift.ExpectedCashAmount);
        Assert.Equal(shift.ExpectedCashAmount, new FinanceService(verify).CalculateExpectedCash(shift));
    }
}
