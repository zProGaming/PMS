using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Pages.System.WorkflowReadiness;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IReadOnlyList<WorkflowReadinessGroup> Groups { get; private set; } = [];

    public WorkflowReadinessSummary Summary { get; private set; } = new(0, 0, 0, 0, 100);

    public async Task OnGetAsync()
    {
        var rooms = await context.Rooms.AsNoTracking().CountAsync();
        var activeRoomStates = await context.Rooms.AsNoTracking().CountAsync(room => room.Status == RoomStatus.Occupied || room.Status == RoomStatus.Dirty || room.Status == RoomStatus.Clean || room.Status == RoomStatus.Inspected);
        var reservations = await context.Reservations.AsNoTracking().CountAsync();
        var checkedIn = await context.Reservations.AsNoTracking().CountAsync(reservation => reservation.Status == ReservationStatus.CheckedIn);
        var checkedOut = await context.Reservations.AsNoTracking().CountAsync(reservation => reservation.Status == ReservationStatus.CheckedOut);
        var folios = await context.Folios.AsNoTracking().CountAsync();
        var folioCharges = await context.FolioItems.AsNoTracking().CountAsync(item => !item.IsVoided);
        var completedPayments = await context.Payments.AsNoTracking().CountAsync(payment => payment.Status == PaymentStatus.Completed);
        var posOrders = await context.POSOrders.AsNoTracking().CountAsync(order => order.OrderStatus == POSOrderStatus.Closed);
        var posRoomCharges = await context.POSOrders.AsNoTracking().CountAsync(order => order.PaymentStatus == POSPaymentStatus.ChargedToRoom);
        var housekeepingTasks = await context.HousekeepingTasks.AsNoTracking().CountAsync();
        var serviceRequests = await context.GuestServiceRequests.AsNoTracking().CountAsync();
        var banquetEvents = await context.BanquetEvents.AsNoTracking().CountAsync();
        var beos = await context.BanquetEventOrders.AsNoTracking().CountAsync();
        var inventoryItems = await context.InventoryItems.AsNoTracking().CountAsync();
        var stockMovements = await context.StockMovements.AsNoTracking().CountAsync();
        var purchaseOrders = await context.PurchaseOrders.AsNoTracking().CountAsync();
        var receivingRecords = await context.ReceivingRecords.AsNoTracking().CountAsync();
        var arInvoices = await context.ARInvoices.AsNoTracking().CountAsync();
        var apInvoices = await context.APInvoices.AsNoTracking().CountAsync();
        var postedOperationalJournals = await context.JournalEntries.AsNoTracking().CountAsync(entry => entry.Status == JournalEntryStatus.Posted && entry.SourceModule != SourceModule.Manual);
        var demoCloseJournals = await context.JournalEntries.AsNoTracking().CountAsync(entry => entry.Status == JournalEntryStatus.Posted && entry.JournalNumber.StartsWith("DEMO-CLOSE-"));
        var bankReconciliations = await context.BankReconciliations.AsNoTracking().CountAsync(reconciliation => reconciliation.Status == BankReconciliationStatus.Approved && reconciliation.Difference == 0);
        var validationCritical = await context.DataValidationIssues.AsNoTracking().CountAsync(issue => !issue.IsResolved && issue.Severity == SystemSeverity.Critical);

        Groups =
        [
            Group("Front Office Stay Cycle",
            [
                Signal("Rooms configured", rooms, "Physical room inventory exists for assignment and room-rack review."),
                Signal("Reservations exist", reservations, "Reservation records are available for front-desk queue testing."),
                Signal("Checked-in stay", checkedIn, "At least one in-house stay exists for folio and service workflows."),
                Signal("Checked-out stay", checkedOut, "At least one completed stay exists for checkout and reporting review."),
                Signal("Folio chain", folios, "Guest folios exist for billing control."),
                Signal("Charges and payments", Math.Min(folioCharges, completedPayments), "Folio charges and completed payments exist for balance testing.")
            ]),
            Group("F&B Charge-to-Room",
            [
                Signal("Closed POS orders", posOrders, "Closed outlet orders exist for settlement review."),
                Signal("Room-charge POS orders", posRoomCharges, "F&B charge-to-room path has evidence."),
                Signal("Folio charge evidence", await context.FolioItems.AsNoTracking().CountAsync(item => item.ChargeCode == "FB" && item.Description.Contains("Order #")), "Room-charge display item exists on a folio.")
            ]),
            Group("Housekeeping & Guest Requests",
            [
                Signal("Room state variety", activeRoomStates, "Rooms have operational readiness states."),
                Signal("Housekeeping tasks", housekeepingTasks, "Task queue is available for supervisor review."),
                Signal("Guest service requests", serviceRequests, "Guest request flow has records to inspect.")
            ]),
            Group("Banquet & Commercial Events",
            [
                Signal("Banquet events", banquetEvents, "Event records exist for banquet workflow review."),
                Signal("BEOs", beos, "Banquet event orders exist for print and approval review.")
            ]),
            Group("Inventory & Purchasing",
            [
                Signal("Inventory items", inventoryItems, "Stock master data exists."),
                Signal("Purchase orders", purchaseOrders, "Purchasing workflow has purchase orders."),
                Signal("Receiving records", receivingRecords, "Receiving workflow has posted or draft records."),
                Signal("Stock movements", stockMovements, "Inventory ledger movement exists.")
            ]),
            Group("Finance Close & Accounting",
            [
                Signal("AR invoices", arInvoices, "City ledger/AR reporting has invoice evidence."),
                Signal("AP invoices", apInvoices, "Supplier liability workflow has AP invoice evidence."),
                Signal("Posted operational journals", postedOperationalJournals, "Operational sources are represented in GL posting."),
                Signal("Clean finance close pack", demoCloseJournals, "DEMO-CLOSE posted finance scenario exists."),
                Signal("Approved bank reconciliation", bankReconciliations, "Zero-difference approved bank reconciliation exists."),
                Signal("No critical health blockers", validationCritical == 0 ? 1 : 0, validationCritical == 0 ? "No unresolved critical data-validation issue is open." : $"{validationCritical} critical issue(s) need resolution.", validationCritical == 0 ? WorkflowSignalStatus.Ready : WorkflowSignalStatus.Attention)
            ])
        ];

        var signals = Groups.SelectMany(group => group.Signals).ToList();
        var ready = signals.Count(signal => signal.Status == WorkflowSignalStatus.Ready);
        var attention = signals.Count(signal => signal.Status == WorkflowSignalStatus.Attention);
        var needsData = signals.Count(signal => signal.Status == WorkflowSignalStatus.NeedsData);
        var score = signals.Count == 0 ? 100 : (int)Math.Round(ready * 100m / signals.Count);
        Summary = new WorkflowReadinessSummary(signals.Count, ready, needsData, attention, score);
    }

    private static WorkflowReadinessGroup Group(string name, IReadOnlyList<WorkflowSignal> signals) => new(name, signals);

    private static WorkflowSignal Signal(string name, int count, string evidence)
        => new(name, count, evidence, count > 0 ? WorkflowSignalStatus.Ready : WorkflowSignalStatus.NeedsData);

    private static WorkflowSignal Signal(string name, int count, string evidence, WorkflowSignalStatus status)
        => new(name, count, evidence, status);

    public static string StatusClass(WorkflowSignalStatus status) => status switch
    {
        WorkflowSignalStatus.Ready => "vpms-status-pill success",
        WorkflowSignalStatus.Attention => "vpms-status-pill danger",
        WorkflowSignalStatus.NeedsData => "vpms-status-pill warning",
        _ => "vpms-status-pill"
    };
}

public enum WorkflowSignalStatus
{
    Ready,
    NeedsData,
    Attention
}

public record WorkflowReadinessGroup(string Name, IReadOnlyList<WorkflowSignal> Signals);

public record WorkflowSignal(string Name, int Count, string Evidence, WorkflowSignalStatus Status);

public record WorkflowReadinessSummary(int Total, int Ready, int NeedsData, int Attention, int Score);
