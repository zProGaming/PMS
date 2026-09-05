using System.Security.Claims;
using Vantage.PMS.Authorization;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Services;
using Xunit;

namespace Vantage.PMS.Tests;

public class CheckoutReviewTests
{
    internal static ClaimsPrincipal User(string role) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "checkout-test-user"), new Claim(ClaimTypes.Name, "test.operator@example.invalid"),
        new Claim(ClaimTypes.Role, role), new Claim("Vantage.CompanyName", "Training property"), new Claim("Vantage.CompanyCode", "QA")
    }, "Identity.Application"));

    internal static Reservation Stay(params decimal[] balances) => new()
    {
        Id = 1, Status = ReservationStatus.CheckedIn, RoomId = 1, Room = new Room { Id = 1 },
        Folios = balances.Select((balance, index) => new Folio
        {
            Id = index + 1, Items = [new FolioItem { Id = index + 1, Amount = 1000 }],
            Payments = [new Payment { Id = index + 1, Amount = 1000 - balance, Status = PaymentStatus.Completed }]
        }).ToList()
    };

    [Fact] public void DebtAndCreditsNeverCancelAcrossFolios()
    {
        var review = new CheckoutReview(Stay(0, 400, -700));
        Assert.Equal(400, review.AmountDue);
        Assert.Equal(700, review.GuestCredit);
        Assert.NotEmpty(review.Validate(new(review.Token, false, null, true), User(PmsRoles.FrontDesk)));
    }

    [Theory]
    [InlineData(PmsRoles.FrontDesk, false)]
    [InlineData(PmsRoles.Cashier, false)]
    [InlineData(PmsRoles.Housekeeper, false)]
    [InlineData(PmsRoles.FrontOfficeManager, true)]
    [InlineData(PmsRoles.FinanceManager, true)]
    [InlineData(PmsRoles.GeneralManager, true)]
    [InlineData(PmsRoles.SystemAdmin, true)]
    public void OverrideUsesServerRole(string role, bool allowed)
    {
        var review = new CheckoutReview(Stay(400));
        Assert.Equal(allowed, review.Validate(new(review.Token, true, "Finance will collect tomorrow.", false), User(role)).Count == 0);
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("         ")] [InlineData("too short")]
    public void ManagerMustRecordMeaningfulReason(string? reason)
    {
        var review = new CheckoutReview(Stay(400));
        Assert.Contains(review.Validate(new(review.Token, true, reason, false), User(PmsRoles.GeneralManager)), e => e.Contains("10–500"));
    }

    [Fact] public void CreditNeedsAcknowledgementButNotDebtOverride()
    {
        var review = new CheckoutReview(Stay(-100));
        Assert.Single(review.Validate(new(review.Token, false, null, false), User(PmsRoles.FrontDesk)));
        Assert.Empty(review.Validate(new(review.Token, false, null, true), User(PmsRoles.FrontDesk)));
    }

    [Fact] public void ChangedPaymentInvalidatesReview()
    {
        var review = new CheckoutReview(Stay(0));
        var oldToken = review.Token;
        review.Reservation.Folios.First().Payments.First().Status = PaymentStatus.Voided;
        Assert.NotEqual(oldToken, review.Token);
        Assert.Contains(review.Validate(new(oldToken, true, "Finance will collect tomorrow.", false), User(PmsRoles.GeneralManager)), e => e.Contains("changed"));
    }

    [Fact] public void TransferredAndVoidedFoliosUseTheirOwnWorkflows()
    {
        var stay = Stay(200, -100);
        stay.Folios.First().Status = FolioStatus.Transferred;
        stay.Folios.Last().Status = FolioStatus.Voided;
        var review = new CheckoutReview(stay);
        Assert.Equal(0, review.AmountDue);
        Assert.Equal(0, review.GuestCredit);
    }

    [Fact] public void ClosedUnsettledFolioCannotBeOverridden()
    {
        var stay = Stay(50);
        stay.Folios.First().Status = FolioStatus.Closed;
        var review = new CheckoutReview(stay);
        Assert.Contains(review.Validate(new(review.Token, true, "Finance will collect tomorrow.", true), User(PmsRoles.GeneralManager)), e => e.Contains("closed folio"));
    }

    [Fact] public void MissingLedgerBlocksCheckout()
    {
        var review = new CheckoutReview(Stay());
        Assert.NotEmpty(review.Validate(new(review.Token, false, null, false), User(PmsRoles.FrontDesk)));
    }

    [Theory]
    [InlineData(PmsRoles.FrontDesk, "front-desk")]
    [InlineData(PmsRoles.Cashier, "cashier")]
    [InlineData(PmsRoles.Housekeeper, "housekeeping")]
    [InlineData(PmsRoles.GeneralManager, "manager")]
    public void HomeDefaultsToTheUsersRole(string role, string expected) =>
        Assert.Equal(expected, Pages.IndexModel.AllowedWorkspaces(User(role)).First().Id);

    [Fact] public void OtherDepartmentsReceiveNoPrivilegedQueues() =>
        Assert.Empty(Pages.IndexModel.AllowedWorkspaces(User(PmsRoles.FBServer)));
}
