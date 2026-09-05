using System.Net;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.Housekeeping;
using Xunit;

namespace Vantage.PMS.Tests;

[Collection("Checkout SQL")]
public class DesktopWorkflowTests(CheckoutDatabase database)
{
    internal static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Vantage.PMS.csproj"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    [Fact] public async Task DesktopQueuesAndCheckoutFitSupportedWorkspaces()
    {
        var checkoutId = await database.SeedStayAsync(0, 400, -700);
        for (var i = 0; i < 9; i++) await database.SeedStayAsync(100);
        await SeedWorkQueuesAsync(checkoutId);
        await using var factory = new DesktopTestHost(database);
        factory.UseKestrel(0);
        using var client = factory.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
        var screenshots = Path.Combine(RepositoryRoot(), "artifacts", "desktop-qa");
        Directory.CreateDirectory(screenshots);
        // 1093x614 CSS pixels at 1.25 scale approximates a 1366x768 desktop at 125%.
        foreach (var size in new[] { (1366, 768, 1f), (1920, 1080, 1f), (1093, 614, 1.25f) })
        {
            await using var browserContext = await browser.NewContextAsync(new()
            {
                BaseURL = client.BaseAddress!.ToString(), ViewportSize = new() { Width = size.Item1, Height = size.Item2 }, DeviceScaleFactor = size.Item3
            });
            var page = await browserContext.NewPageAsync();
            var scriptErrors = new List<string>();
            page.PageError += (_, error) => scriptErrors.Add(error);
            foreach (var role in new[] { PmsRoles.FrontDesk, PmsRoles.Cashier, PmsRoles.Housekeeper, PmsRoles.GeneralManager })
            {
                await browserContext.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["X-Test-Role"] = role });
                Assert.Equal(200, (await page.GotoAsync("/"))!.Status);
                await Assertions.Expect(page.Locator(".daily-work h1")).ToHaveTextAsync("Daily Work");
                Assert.Equal(0, await page.Locator(".daily-work a:not([href])").CountAsync());
                if (size.Item1 == 1366)
                {
                    foreach (var link in await page.Locator(".work-queue-heading > a, .work-panel tbody tr:first-child .work-next a").AllAsync())
                    {
                        var href = await link.GetAttributeAsync("href");
                        var linkedResponse = await page.APIRequest.GetAsync(href!);
                        Assert.True(linkedResponse.Status == 200, $"Queue link {href} returned {linkedResponse.Status} for {role}.");
                    }
                }
                Assert.True(await page.Locator(".work-table tbody tr").CountAsync() <= Pages.IndexModel.QueueLimit * 3);
                await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, $"{role}-{size.Item1}.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
                await AssertNoPageOverflow(page);
            }
            await browserContext.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["X-Test-Role"] = PmsRoles.GeneralManager });
            Assert.Equal(200, (await page.GotoAsync($"/FrontOffice/Reservations/CheckOut/{checkoutId}"))!.Status);
            await Assertions.Expect(page.Locator("[data-vpms-checkout-submit]")).ToBeDisabledAsync();
            await page.Locator("[data-vpms-checkout-override]").CheckAsync();
            await Assertions.Expect(page.Locator("[data-vpms-checkout-submit]")).ToBeDisabledAsync();
            await page.Locator("[data-vpms-checkout-reason]").FillAsync("Finance will collect tomorrow and review the guest credit.");
            await Assertions.Expect(page.Locator("[data-vpms-checkout-submit]")).ToBeDisabledAsync();
            await page.Locator("[data-vpms-checkout-credit]").CheckAsync();
            await Assertions.Expect(page.Locator("[data-vpms-checkout-submit]")).ToBeEnabledAsync();
            await AssertNoPageOverflow(page);
            await page.EvaluateAsync("window.scrollTo({top: 0, behavior: 'instant'})");
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, $"checkout-{size.Item1}.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
            Assert.Empty(scriptErrors);
        }
    }

    [Fact] public async Task BrowserCompletesSettledCheckoutWithAntiforgery()
    {
        var id = await database.SeedStayAsync(0);
        await using var factory = new DesktopTestHost(database);
        factory.UseKestrel(0);
        using var client = factory.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
        await using var browserContext = await browser.NewContextAsync(new()
        {
            BaseURL = client.BaseAddress!.ToString(), ViewportSize = new() { Width = 1366, Height = 768 },
            ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Test-Role"] = PmsRoles.FrontDesk }
        });
        var page = await browserContext.NewPageAsync();
        await page.GotoAsync($"/FrontOffice/Reservations/CheckOut/{id}");
        await page.Locator("[data-vpms-checkout-submit]").ClickAsync();
        await page.WaitForURLAsync($"**/FrontOffice/Reservations/Details/{id}");
        await using var verify = database.Open();
        Assert.Equal(ReservationStatus.CheckedOut, (await verify.Reservations.FindAsync(id))!.Status);
    }

    [Fact] public async Task RouteAuthorizationAndAntiforgeryAreEnforced()
    {
        var id = await database.SeedStayAsync(100);
        await using var factory = new DesktopTestHost(database);
        using var cashier = factory.CreateClient(new() { AllowAutoRedirect = false });
        cashier.DefaultRequestHeaders.Add("X-Test-Role", PmsRoles.Cashier);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/?workspace=manager")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/Overview")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync($"/FrontOffice/Reservations/CheckOut/{id}")).StatusCode);
        using var desk = factory.CreateClient(new() { AllowAutoRedirect = false });
        desk.DefaultRequestHeaders.Add("X-Test-Role", PmsRoles.FrontDesk);
        Assert.Equal(HttpStatusCode.BadRequest, (await desk.PostAsync($"/FrontOffice/Reservations/CheckOut/{id}", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode);
        var html = await desk.GetStringAsync($"/FrontOffice/Reservations/CheckOut/{id}");
        Assert.DoesNotContain("data-vpms-checkout-override", html);
        Assert.Contains("Collect payment", html);
        var home = await desk.GetStringAsync("/");
        Assert.DoesNotContain("Voids awaiting approval", home);
        Assert.DoesNotContain("Collection and guest-credit follow-up", home);
        // A forged checkbox with a VALID antiforgery token still cannot grant a role.
        var token = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var reviewToken = System.Text.RegularExpressions.Regex.Match(html, "id=\"ReviewToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        Assert.NotEmpty(reviewToken);
        var forged = await desk.PostAsync($"/FrontOffice/Reservations/CheckOut/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token), ["ReviewToken"] = reviewToken,
            ["ManagerOverrideRequested"] = "true", ["OverrideReason"] = "Forged approval must not succeed."
        }));
        Assert.Equal(HttpStatusCode.OK, forged.StatusCode);
        Assert.Contains("Collect the amount due", await forged.Content.ReadAsStringAsync());
        await using var verify = database.Open();
        Assert.Equal(ReservationStatus.CheckedIn, (await verify.Reservations.FindAsync(id))!.Status);
    }

    internal static async Task AssertNoPageOverflow(IPage page)
    {
        var overflowDetails = await page.EvaluateAsync<string>("""
            () => JSON.stringify([...document.querySelectorAll('body *')]
              .filter(e => e.getBoundingClientRect().width && e.getBoundingClientRect().right > innerWidth + 1)
              .slice(0, 12).map(e => ({ tag: e.tagName, css: e.className, right: e.getBoundingClientRect().right, viewport: innerWidth })))
            """);
        Assert.False(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > window.innerWidth + 1"), "Desktop overflow: " + overflowDetails);
        var clippedLabels = await page.EvaluateAsync<string[]>("""
            () => [...document.querySelectorAll('.work-heading h1, .work-panel h2, .work-panel h3, .work-tabs a, .work-actions .btn, .work-facts dd')]
              .filter(e => e.clientWidth && (e.scrollWidth > e.clientWidth + 1 || e.scrollHeight > e.clientHeight + 1))
              .map(e => e.textContent.trim())
            """);
        Assert.Empty(clippedLabels);
    }

    private async Task SeedWorkQueuesAsync(int reservationId)
    {
        await using var context = database.Open();
        var stay = await context.Reservations.Include(r => r.Room).Include(r => r.Folios).SingleAsync(r => r.Id == reservationId);
        var folioId = stay.Folios.First().Id;
        context.VoidRequests.Add(new() { ReferenceType = "Folio", ReferenceId = folioId, Reason = "Review a duplicate training entry.", RequestedBy = "Training cashier" });
        context.RefundTransactions.Add(new() { FolioId = folioId, RefundNumber = "QA-REFUND", Amount = 100, Reason = "Training credit review", RequestedBy = "Training cashier" });
        context.DiscountApprovals.Add(new() { FolioId = folioId, DiscountAmount = 50, Reason = "Training service recovery", RequestedBy = "Training front desk" });
        var room = new Room { PropertyId = stay.PropertyId, RoomTypeId = stay.Room!.RoomTypeId, RoomNumber = "QA-1205", Status = RoomStatus.Dirty };
        context.HousekeepingTasks.Add(new() { Room = room, Priority = HousekeepingTaskPriority.High, AssignedTo = "Housekeeping Queue", Notes = "Training turnover" });
        context.CashierShifts.Add(new() { ShiftNumber = "QA-SHIFT", OpenedBy = "test.operator@example.invalid", OpeningCashFloat = 1000 });
        await context.SaveChangesAsync();
    }
}

// Only the TEST ASSEMBLY accepts this synthetic role header. Test authentication
// and database fixtures are excluded from the application's production publish.
internal sealed class DesktopTestHost(CheckoutDatabase database) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing").UseContentRoot(DesktopWorkflowTests.RepositoryRoot()).UseStaticWebAssets()
            .UseSetting("Startup:RunIdentitySeed", "false");
        builder.ConfigureTestServices(services =>
        {
            // The test-only Kestrel listener is loopback HTTP. Production keeps
            // SecurePolicy.Always; antiforgery token validation remains enabled here.
            services.PostConfigure<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>(o =>
                o.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest);
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(database.ConnectionString, sql => sql.EnableRetryOnFailure()));
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = "DesktopTest"; o.DefaultChallengeScheme = "DesktopTest"; o.DefaultForbidScheme = "DesktopTest";
            }).AddScheme<AuthenticationSchemeOptions, DesktopTestAuthentication>("DesktopTest", _ => { });
            services.PostConfigure<LoggerFilterOptions>(o => { o.Rules.Clear(); o.MinLevel = LogLevel.Error; });
        });
    }
}

internal sealed class DesktopTestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers["X-Test-Role"].ToString();
        return Task.FromResult(PmsRoles.All.Contains(role)
            ? AuthenticateResult.Success(new AuthenticationTicket(Request.Headers.TryGetValue("X-Test-Actor", out var actor)
                ? FinanceAdjustmentTests.Actor(role, actor.ToString()) : CheckoutReviewTests.User(role), Scheme.Name))
            : AuthenticateResult.NoResult());
    }
}
