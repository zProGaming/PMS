using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Vantage.PMS.Authorization;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Housekeeping;
using Xunit;

namespace Vantage.PMS.Tests;

[Collection("Checkout SQL")]
public class SettlementDesktopTests(CheckoutDatabase database)
{
    [Fact] public async Task CashierCanSettleWithoutReservationAccessAndDesktopLabelsPreserveGuestData()
    {
        var stayId = await database.SeedStayAsync(400);
        var actor = "desktop.cashier-" + Guid.NewGuid().ToString("N");
        int folioId, roomId;
        await using (var context = database.Open())
        {
            var stay = await context.Reservations.Include(r => r.Folios).Include(r => r.Room).SingleAsync(r => r.Id == stayId);
            folioId = stay.Folios.Single().Id;
            context.CashierShifts.Add(new CashierShift { OpenedBy = actor, ShiftNumber = actor, OpeningCashFloat = 1000 });
            var room = new Room { PropertyId = stay.PropertyId, RoomTypeId = stay.Room!.RoomTypeId, RoomNumber = "QA-DESKTOP", Status = RoomStatus.Dirty };
            context.HousekeepingTasks.Add(new HousekeepingTask { Room = room, AssignedTo = "Training cleaner" });
            await context.SaveChangesAsync(); roomId = room.Id;
        }
        await using var factory = new DesktopTestHost(database);
        factory.UseKestrel(0);
        using var client = factory.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
        var screenshots = Path.Combine(DesktopWorkflowTests.RepositoryRoot(), "artifacts", "desktop-qa");
        Directory.CreateDirectory(screenshots);
        foreach (var size in new[] { (1366, 768, 1f), (1920, 1080, 1f), (1093, 614, 1.25f) })
        {
            await using var browserContext = await browser.NewContextAsync(new()
            {
                BaseURL = client.BaseAddress!.ToString(), ViewportSize = new() { Width = size.Item1, Height = size.Item2 }, DeviceScaleFactor = size.Item3,
                ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Test-Role"] = PmsRoles.Cashier, ["X-Test-Actor"] = actor }
            });
            var page = await browserContext.NewPageAsync();
            var errors = new List<string>(); page.PageError += (_, e) => errors.Add(e);
            Assert.Equal(200, (await page.GotoAsync($"/Finance/Settlement?folioId={folioId}"))!.Status);
            await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Cashier Settlement");
            Assert.Contains("De la Cruz-Santos (training guest)", await page.Locator("main").InnerTextAsync());
            await DesktopWorkflowTests.AssertNoPageOverflow(page);
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, $"settlement-{size.Item1}.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
            await page.GetByRole(AriaRole.Link, new() { Name = "Collect Payment", Exact = true }).ClickAsync();
            await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Post Payment");
            Assert.StartsWith("/Finance/Settlement/PostPayment", await page.Locator("main form[method=post]").GetAttributeAsync("action"));
            await Assertions.Expect(page.Locator("label[for=Payment_PaymentDate]")).ToHaveTextAsync("Payment Date");
            await DesktopWorkflowTests.AssertNoPageOverflow(page);
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, $"collect-payment-{size.Item1}.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
            Assert.Equal(403, (await page.APIRequest.GetAsync($"/FrontOffice/Reservations/Edit/{stayId}" )).Status);
            Assert.Equal(400, (await page.APIRequest.PostAsync($"/Finance/Settlement/PostPayment/{folioId}")).Status);
            var native = await page.APIRequest.GetAsync($"/Finance/Settlement/PostPayment/{folioId}?handler=Native");
            Assert.Equal(200, native.Status);
            Assert.Contains($"/Finance/Settlement/PostPayment/{folioId}", await native.TextAsync());

            await page.GotoAsync("/Finance/Refunds?handler=CreateNative");
            await Assertions.Expect(page.Locator("label[for=Refund_RequestedBy]")).ToHaveTextAsync("Requested By");
            await Assertions.Expect(page.Locator("select#Refund_RefundMethod option[value='1']")).ToHaveTextAsync("Credit Card");
            await Assertions.Expect(page.Locator("#Refund_RequestedBy")).ToHaveValueAsync(actor);
            await browserContext.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["X-Test-Role"] = PmsRoles.HousekeepingSupervisor, ["X-Test-Actor"] = "training.supervisor" });
            Assert.Equal(200, (await page.GotoAsync($"/Housekeeping/Rooms/UpdateStatus/{roomId}"))!.Status);
            Assert.Equal("Dirty", await page.Locator("#ExpectedStatus").InputValueAsync());
            await DesktopWorkflowTests.AssertNoPageOverflow(page);
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, $"housekeeping-handoff-{size.Item1}.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
            Assert.Empty(errors);
        }
        // Exercise a real antiforgery-protected cashier POST, not just a direct service call.
        await using var postingContext = await browser.NewContextAsync(new()
        {
            BaseURL = client.BaseAddress!.ToString(), ViewportSize = new() { Width = 1366, Height = 768 },
            ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Test-Role"] = PmsRoles.Cashier, ["X-Test-Actor"] = actor }
        });
        var postingPage = await postingContext.NewPageAsync();
        await postingPage.GotoAsync($"/Finance/Settlement/PostPayment/{folioId}");
        await postingPage.Locator("#Payment_PaymentMethod").FillAsync("Cash");
        await postingPage.Locator("#Payment_ReferenceNumber").FillAsync("QA-DESKTOP-" + Guid.NewGuid().ToString("N"));
        await postingPage.GetByRole(AriaRole.Button, new() { Name = "Post Payment", Exact = true }).ClickAsync();
        await postingPage.WaitForURLAsync("**/Finance/Settlement?folioId=*");
        await using var verify = database.Open();
        var folio = await verify.Folios.Include(f => f.Items).Include(f => f.Payments).SingleAsync(f => f.Id == folioId);
        Assert.Equal(0, folio.Balance);
        Assert.Equal(2, folio.Payments.Count);
    }
}
