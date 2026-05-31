using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.System.PilotLaunch;

public class IndexModel(ApplicationDbContext context, ReportCatalogService reportCatalog) : PageModel
{
    public PilotLaunchSnapshot Snapshot { get; private set; } = new();

    public IReadOnlyList<PilotLaunchGate> Gates { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var criticalIssues = await context.DataValidationIssues.AsNoTracking().CountAsync(issue => !issue.IsResolved && issue.Severity == SystemSeverity.Critical);
        var highIssues = await context.DataValidationIssues.AsNoTracking().CountAsync(issue => !issue.IsResolved && issue.Severity == SystemSeverity.High);
        var businessDate = await context.BusinessDateSettings.AsNoTracking().Select(setting => (DateTime?)setting.CurrentBusinessDate).FirstOrDefaultAsync();
        var rooms = await context.Rooms.AsNoTracking().CountAsync();
        var reservations = await context.Reservations.AsNoTracking().CountAsync();
        var checkedIn = await context.Reservations.AsNoTracking().CountAsync(reservation => reservation.Status == ReservationStatus.CheckedIn);
        var folios = await context.Folios.AsNoTracking().CountAsync();
        var payments = await context.Payments.AsNoTracking().CountAsync(payment => payment.Status == PaymentStatus.Completed);
        var posChargedToRoom = await context.POSOrders.AsNoTracking().CountAsync(order => order.PaymentStatus == POSPaymentStatus.ChargedToRoom);
        var postedOperationalJournals = await context.JournalEntries.AsNoTracking().CountAsync(entry => entry.Status == JournalEntryStatus.Posted && entry.SourceModule != SourceModule.Manual);
        var cleanCloseJournals = await context.JournalEntries.AsNoTracking().CountAsync(entry => entry.Status == JournalEntryStatus.Posted && entry.JournalNumber.StartsWith("DEMO-CLOSE-"));
        var approvedBankReconciliations = await context.BankReconciliations.AsNoTracking().CountAsync(reconciliation => reconciliation.Status == BankReconciliationStatus.Approved && reconciliation.Difference == 0);
        var availableReports = reportCatalog.GetCatalog().Count(report => report.IsAvailable);

        Gates =
        [
            Gate("System Health", criticalIssues == 0, criticalIssues == 0 ? "No unresolved critical validation issue." : $"{criticalIssues} unresolved critical issue(s).", "/System/HealthCheck/Index"),
            Gate("High-Risk Queue", highIssues <= 5, highIssues <= 5 ? "High-risk queue is within trial review tolerance." : $"{highIssues} high-risk issue(s) need triage.", "/System/HealthCheck/Index"),
            Gate("Core Stay Workflow", rooms > 0 && reservations > 0 && checkedIn > 0 && folios > 0, $"Rooms {rooms}, reservations {reservations}, in-house {checkedIn}, folios {folios}.", "/System/WorkflowReadiness/Index"),
            Gate("Cashiering Evidence", payments > 0, $"{payments} completed payment(s) found.", "/Finance/Index"),
            Gate("POS to Room Evidence", posChargedToRoom > 0, $"{posChargedToRoom} charged-to-room POS order(s) found.", "/FoodBeverage/Index"),
            Gate("Accounting Posting Evidence", postedOperationalJournals > 0, $"{postedOperationalJournals} posted operational journal(s) found.", "/Accounting/Index"),
            Gate("Finance Close Pack", cleanCloseJournals > 0, $"{cleanCloseJournals} DEMO-CLOSE journal(s) found.", "/Admin/DemoSetup/Index"),
            Gate("Treasury Evidence", approvedBankReconciliations > 0, $"{approvedBankReconciliations} approved zero-difference bank reconciliation(s).", "/Accounting/Banking/BankReconciliations/Index"),
            Gate("Report Center", availableReports > 0, $"{availableReports} available catalog report(s).", "/System/ReportReadiness/Index")
        ];

        var passed = Gates.Count(gate => gate.IsPassed);
        var score = Gates.Count == 0 ? 100 : (int)Math.Round(passed * 100m / Gates.Count);
        Snapshot = new PilotLaunchSnapshot(score, passed, Gates.Count - passed, businessDate, criticalIssues, highIssues, availableReports);
    }

    private static PilotLaunchGate Gate(string name, bool passed, string evidence, string routePath)
        => new(name, passed, evidence, routePath);

    public static string GateClass(PilotLaunchGate gate) => gate.IsPassed ? "vpms-status-pill success" : "vpms-status-pill danger";
}

public record PilotLaunchSnapshot(
    int Score = 100,
    int PassedGates = 0,
    int OpenGates = 0,
    DateTime? BusinessDate = null,
    int CriticalIssues = 0,
    int HighIssues = 0,
    int AvailableReports = 0);

public record PilotLaunchGate(string Name, bool IsPassed, string Evidence, string RoutePath);
