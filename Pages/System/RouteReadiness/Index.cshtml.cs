using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vantage.PMS.Pages.System.RouteReadiness;

public class IndexModel(IWebHostEnvironment environment) : PageModel
{
    public IReadOnlyList<RouteSmokeGroup> Groups { get; private set; } = [];

    public RouteSmokeSummary Summary { get; private set; } = new(0, 0, 0, 0, 100);

    public void OnGet()
    {
        Groups = BuildCandidates()
            .GroupBy(candidate => candidate.Module)
            .Select(group => new RouteSmokeGroup(group.Key, group.Select(InspectRoute).ToList()))
            .ToList();

        var items = Groups.SelectMany(group => group.Items).ToList();
        var missing = items.Count(item => item.Status == RouteSmokeStatus.Missing);
        var review = items.Count(item => item.Status == RouteSmokeStatus.NeedsReview);
        var ready = items.Count(item => item.Status == RouteSmokeStatus.Ready);
        var score = items.Count == 0 ? 100 : Math.Max(0, (int)Math.Round((ready * 100m) / items.Count));
        Summary = new RouteSmokeSummary(items.Count, ready, review, missing, score);
    }

    private RouteSmokeResult InspectRoute(RouteSmokeCandidate candidate)
    {
        var relativePath = ToPageFilePath(candidate.RoutePath);
        var absolutePath = global::System.IO.Path.Combine(environment.ContentRootPath, relativePath);
        if (!global::System.IO.File.Exists(absolutePath))
        {
            return new RouteSmokeResult(candidate, RouteSmokeStatus.Missing, relativePath, "No Razor page file was found for this route.");
        }

        var content = global::System.IO.File.ReadAllText(absolutePath);
        var lower = content.ToLowerInvariant();
        var unavailableSignals = new[] { "planned", "not yet configured", "placeholder", "awaiting", "coming soon" };
        if (unavailableSignals.Any(signal => lower.Contains(signal)))
        {
            return new RouteSmokeResult(candidate, RouteSmokeStatus.NeedsReview, relativePath, "Page exists, but contains unavailable-language signals.");
        }

        return new RouteSmokeResult(candidate, RouteSmokeStatus.Ready, relativePath, "Razor page exists.");
    }

    private static string ToPageFilePath(string routePath)
    {
        var clean = routePath.Split('?', '#')[0].Trim('/');
        if (string.IsNullOrWhiteSpace(clean))
        {
            clean = "Index";
        }

        return global::System.IO.Path.Combine(["Pages", .. clean.Split('/', StringSplitOptions.RemoveEmptyEntries)]) + ".cshtml";
    }

    private static IReadOnlyList<RouteSmokeCandidate> BuildCandidates() =>
    [
        Item("Command Center", "Enterprise Dashboard", "/Index", "Executive opening screen"),
        Item("Command Center", "Pilot Readiness & System Health", "/System/HealthCheck/Index", "Audit control surface"),
        Item("Command Center", "Route Smoke Console", "/System/RouteReadiness/Index", "Phase 6 route matrix"),
        Item("Command Center", "Management AI", "/ManagementAI/Index", "Rule-based management insights"),
        Item("Command Center", "Executive Dashboard", "/Executive/Index", "Owner and GM command view"),

        Item("Front Office", "Front Office Dashboard", "/FrontOffice/Index", "Front-desk operating view"),
        Item("Front Office", "Reservations List", "/FrontOffice/Reservations/Index", "Reservation queue"),
        Item("Front Office", "Create Reservation", "/FrontOffice/Reservations/Create", "New booking workflow"),
        Item("Front Office", "Room Calendar", "/FrontOffice/RoomRack/Index", "Grid room calendar"),
        Item("Front Office", "Guest Registry", "/FrontOffice/Guests/Index", "Guest master list"),
        Item("Front Office", "Folio List", "/FrontOffice/Folios/Index", "Guest folio landing"),

        Item("Rooms Operations", "Housekeeping Board", "/Housekeeping/Index", "Room readiness board"),
        Item("Rooms Operations", "Housekeeping Tasks", "/Housekeeping/Tasks/Index", "Task assignment queue"),
        Item("Rooms Operations", "Guest Service Requests", "/GuestPortalManagement/ServiceRequests/Index", "Guest request queue"),

        Item("Finance Control", "Finance Dashboard", "/Finance/Index", "Cashiering and finance control"),
        Item("Finance Control", "Payments", "/Finance/Payments/Index", "Payment register"),
        Item("Finance Control", "Cashier Shifts", "/Finance/CashierShifts/Index", "Shift accountability"),
        Item("Finance Control", "Night Audit", "/Finance/NightAudit/Index", "Business date close"),
        Item("Finance Control", "Finance Documents", "/Finance/Documents/Index", "Finance document register"),
        Item("Finance Control", "AR Aging", "/AccountsReceivable/Aging/Index", "Receivables aging"),
        Item("Finance Control", "AR Collections", "/AccountsReceivable/Collections/Index", "Collection intelligence"),

        Item("Accounting", "Accounting Dashboard", "/Accounting/Index", "GL command view"),
        Item("Accounting", "Chart of Accounts", "/Accounting/ChartOfAccounts/Index", "COA setup"),
        Item("Accounting", "Journal Entries", "/Accounting/JournalEntries/Index", "Journal register"),
        Item("Accounting", "Trial Balance", "/Accounting/Reports/TrialBalance", "Trial balance report"),
        Item("Accounting", "Balance Sheet", "/Accounting/Reports/BalanceSheet", "Balance sheet report"),
        Item("Accounting", "Profit and Loss", "/Accounting/Reports/ProfitAndLoss", "P&L report"),
        Item("Accounting", "Statement of Cash Flows", "/Accounting/Reports/StatementOfCashFlows", "Cash flow report"),
        Item("Accounting", "USALI Operating Statement", "/Accounting/Reports/USALI", "USALI-style report"),
        Item("Accounting", "Accounts Payable", "/Accounting/AccountsPayable/Index", "AP dashboard"),
        Item("Accounting", "Bank Reconciliation", "/Accounting/Banking/BankReconciliations/Index", "Treasury reconciliation"),

        Item("F&B and Kitchen", "POS Dashboard", "/FoodBeverage/Index", "Outlet operations"),
        Item("F&B and Kitchen", "New POS Order", "/FoodBeverage/Orders/Create", "Order entry"),
        Item("F&B and Kitchen", "POS Orders", "/FoodBeverage/Orders/Index", "Order register"),
        Item("F&B and Kitchen", "Kitchen Display", "/FoodBeverageKitchen/Index", "Kitchen production board"),
        Item("F&B and Kitchen", "Kitchen Stations", "/FoodBeverageKitchen/KitchenStations/Index", "Station setup"),

        Item("Sales and Banquet", "Sales Dashboard", "/Sales/Index", "Sales CRM overview"),
        Item("Sales and Banquet", "Banquet Dashboard", "/Banquet/Index", "Banquet command view"),
        Item("Sales and Banquet", "Event Calendar", "/Banquet/Calendar/Index", "Event calendar"),
        Item("Sales and Banquet", "Banquet Events", "/Banquet/Events/Index", "Event list"),
        Item("Sales and Banquet", "BEOs", "/Banquet/BEOs/Index", "Banquet event orders"),

        Item("Revenue and Booking", "Revenue Dashboard", "/Revenue/Index", "Revenue command view"),
        Item("Revenue and Booking", "Revenue Calendar", "/Revenue/Calendar/Index", "Rate calendar"),
        Item("Revenue and Booking", "Booking Requests", "/BookingManagement/Requests/Index", "Direct booking requests"),
        Item("Revenue and Booking", "Public Booking Landing", "/Booking/Index", "Public booking entry"),
        Item("Revenue and Booking", "Guest Portal", "/GuestPortal/Index", "Public guest self-service"),

        Item("Inventory and Purchasing", "Inventory Dashboard", "/Inventory/Index", "Inventory control"),
        Item("Inventory and Purchasing", "Inventory Items", "/Inventory/Items/Index", "Stock item master"),
        Item("Inventory and Purchasing", "Purchase Requests", "/Purchasing/PurchaseRequests/Index", "Purchase request queue"),
        Item("Inventory and Purchasing", "Purchase Orders", "/Purchasing/PurchaseOrders/Index", "Purchase order queue"),
        Item("Inventory and Purchasing", "Receiving", "/Purchasing/Receiving/Index", "Receiving workflow"),

        Item("Groups and Documents", "Group Bookings", "/Groups/Index", "Group management"),
        Item("Groups and Documents", "Pseudo Rooms", "/Groups/PseudoRooms/Index", "Paymaster accounts"),
        Item("Groups and Documents", "Printable Documents", "/Documents/Index", "Print center"),
        Item("Groups and Documents", "Report Center", "/Reports/Center", "Curated report catalog"),
        Item("Groups and Documents", "Presentation Mode", "/System/DemoPresentation/Index", "Presentation surface"),
        Item("Groups and Documents", "Workflow Launcher", "/System/DemoWorkflowLauncher/Index", "Workflow launchpad")
    ];

    private static RouteSmokeCandidate Item(string module, string name, string routePath, string purpose)
        => new(module, name, routePath, purpose);

    public static string StatusBadge(RouteSmokeStatus status) => status switch
    {
        RouteSmokeStatus.Ready => "vpms-status-pill success",
        RouteSmokeStatus.NeedsReview => "vpms-status-pill warning",
        RouteSmokeStatus.Missing => "vpms-status-pill danger",
        _ => "vpms-status-pill"
    };
}

public enum RouteSmokeStatus
{
    Ready,
    NeedsReview,
    Missing
}

public record RouteSmokeCandidate(string Module, string Name, string RoutePath, string Purpose);

public record RouteSmokeResult(RouteSmokeCandidate Candidate, RouteSmokeStatus Status, string PageFile, string Evidence);

public record RouteSmokeGroup(string Module, IReadOnlyList<RouteSmokeResult> Items);

public record RouteSmokeSummary(int Total, int Ready, int NeedsReview, int Missing, int Score);
