using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vantage.PMS.Authorization;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;
using Xunit;

namespace Vantage.PMS.Tests;

[Collection("Checkout SQL")]
public class CheckoutIntegrationTests(CheckoutDatabase database)
{
    [Fact] public async Task ConcurrentCheckoutCreatesOneTurnoverAndOneDecision()
    {
        var id = await database.SeedStayAsync(0, 0);
        await using var first = database.Open();
        await using var second = database.Open();
        var review = new CheckoutReview((await new CheckoutService(first).LoadAsync(id))!);
        var request = new CheckoutRequest(review.Token, false, null, false);
        var results = await Task.WhenAll(
            new CheckoutService(first).CompleteAsync(id, request, CheckoutReviewTests.User(PmsRoles.FrontDesk)),
            new CheckoutService(second).CompleteAsync(id, request, CheckoutReviewTests.User(PmsRoles.FrontDesk)));
        Assert.All(results, result => Assert.True(result.Completed));
        await using var verify = database.Open();
        var saved = (await new CheckoutService(verify).LoadAsync(id))!;
        Assert.Equal(ReservationStatus.CheckedOut, saved.Status);
        Assert.Equal(RoomStatus.Dirty, saved.Room!.Status);
        Assert.All(saved.Folios, f => Assert.Equal(FolioStatus.Closed, f.Status));
        Assert.Equal(1, await verify.HousekeepingTasks.CountAsync(t => t.RoomId == saved.RoomId));
        Assert.Equal(1, await verify.AuditLogs.CountAsync(a => a.EntityName == "CheckoutDecision" && a.EntityId == id.ToString()));
    }

    [Fact] public async Task OverrideRecordsActorReasonAndKeepsDebtAndCreditOpen()
    {
        var id = await database.SeedStayAsync(0, 400, -700);
        await using var context = database.Open();
        var service = new CheckoutService(context);
        var review = new CheckoutReview((await service.LoadAsync(id))!);
        var result = await service.CompleteAsync(id, new(review.Token, true, "Finance will collect tomorrow.", true), CheckoutReviewTests.User(PmsRoles.GeneralManager));
        Assert.True(result.Completed);
        var stay = (await service.LoadAsync(id))!;
        Assert.All(stay.Folios.Where(f => f.Balance != 0), f => Assert.Equal(FolioStatus.Open, f.Status));
        var audit = await context.AuditLogs.SingleAsync(a => a.EntityName == "CheckoutDecision" && a.EntityId == id.ToString());
        Assert.Equal("checkout-test-user", audit.UserId);
        Assert.Contains("Finance will collect tomorrow.", audit.NewValues);
        Assert.Contains("\"AmountDue\":400", audit.NewValues);
        Assert.Contains("\"GuestCredit\":700", audit.NewValues);
    }

    [Fact] public async Task PaymentAfterReviewForcesFreshCheckoutDecision()
    {
        var id = await database.SeedStayAsync(100);
        await using var context = database.Open();
        var service = new CheckoutService(context);
        var review = new CheckoutReview((await service.LoadAsync(id))!);
        await using (var paymentContext = database.Open())
        {
            var errors = await new FinanceService(paymentContext).PostFolioPaymentAsync(new Payment
            {
                FolioId = review.RelevantFolios[0].Id, Amount = 100, PaymentMethod = "Cash", ReferenceNumber = Guid.NewGuid().ToString()
            }, "test.operator@example.invalid", true);
            Assert.Empty(errors);
        }
        var result = await service.CompleteAsync(id, new(review.Token, true, "Finance will collect tomorrow.", false), CheckoutReviewTests.User(PmsRoles.GeneralManager));
        Assert.False(result.Completed);
        Assert.Contains(result.Errors, e => e.Contains("changed"));
        Assert.Equal(ReservationStatus.CheckedIn, (await service.LoadAsync(id))!.Status);
    }

    [Fact] public async Task ConcurrentPaymentsCannotOverpayFolio()
    {
        var id = await database.SeedStayAsync(100);
        await using var first = database.Open();
        await using var second = database.Open();
        var folioId = await first.Folios.Where(f => f.ReservationId == id).Select(f => f.Id).SingleAsync();
        Payment Input() => new() { FolioId = folioId, Amount = 100, PaymentMethod = "Cash", ReferenceNumber = Guid.NewGuid().ToString() };
        var results = await Task.WhenAll(new FinanceService(first).PostFolioPaymentAsync(Input(), "QA", true),
            new FinanceService(second).PostFolioPaymentAsync(Input(), "QA", true));
        Assert.Single(results, r => r.Count == 0);
        await using var verify = database.Open();
        Assert.Equal(0, (await new CheckoutService(verify).LoadAsync(id))!.Folios.Single().Balance);
    }

    [Fact] public async Task FailureAfterBusinessSaveRollsBackRoomFolioTurnoverAndAudit()
    {
        var id = await database.SeedStayAsync(0);
        await using var context = database.Open(new FailAfterCheckoutSave());
        var service = new CheckoutService(context);
        var review = new CheckoutReview((await service.LoadAsync(id))!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(id, new(review.Token, false, null, false), CheckoutReviewTests.User(PmsRoles.FrontDesk)));
        await using var verify = database.Open();
        var stay = (await new CheckoutService(verify).LoadAsync(id))!;
        Assert.Equal(ReservationStatus.CheckedIn, stay.Status);
        Assert.Equal(RoomStatus.Occupied, stay.Room!.Status);
        Assert.Equal(FolioStatus.Open, stay.Folios.Single().Status);
        Assert.False(await verify.HousekeepingTasks.AnyAsync(t => t.RoomId == stay.RoomId));
        Assert.False(await verify.AuditLogs.AnyAsync(a => a.EntityName == "CheckoutDecision" && a.EntityId == id.ToString()));
    }

    private sealed class FailAfterCheckoutSave : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AuditLog>().Any(e => e.Entity.EntityName == "CheckoutDecision"))
                throw new InvalidOperationException("Injected checkout failure after database write.");
            return ValueTask.FromResult(result);
        }
    }
}
