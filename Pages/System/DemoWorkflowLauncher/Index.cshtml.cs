using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vantage.PMS.Pages.System.DemoWorkflowLauncher;

public class IndexModel : PageModel
{
    public IList<WorkflowLaunchItem> Items { get; } =
    [
        new("Enterprise Dashboard", "/Index", "Start the executive command center walkthrough."),
        new("Front Office Dashboard", "/FrontOffice/Index", "Arrivals, departures, in-house, and room operations."),
        new("Room Rack", "/Housekeeping/Index", "Visual room status and housekeeping readiness."),
        new("Reservation Demo", "/FrontOffice/Reservations/Index", "Reservation search, create, check-in, and checkout flow."),
        new("Folio Demo", "/FrontOffice/Index", "Open an in-house reservation, then view the guest folio for charges, payments, balance, and printout."),
        new("POS Demo", "/FoodBeverage/Index", "F&B dashboard, orders, room charge, and settlement."),
        new("Kitchen Display", "/FoodBeverageKitchen/Index", "Station-based food preparation status updates."),
        new("Banquet BEO", "/Banquet/BEOs/Index", "Banquet Event Orders and operational instructions."),
        new("Revenue Calendar", "/Revenue/Calendar/Index", "Rates, restrictions, occupancy, and room availability controls."),
        new("Booking Engine", "/Booking/Index", "Anonymous public booking flow."),
        new("Guest Portal", "/GuestPortal/Index", "Guest lookup, pre-check-in, folio estimate, and service requests."),
        new("Inventory Dashboard", "/Inventory/Index", "Stock, low-stock, purchase flow, and movements."),
        new("AR Aging", "/AccountsReceivable/Aging/Index", "City ledger and aging risk view."),
        new("Management AI Dashboard", "/ManagementAI/Index", "Rule-based daily summary and recommendations."),
        new("Audit Logs", "/System/AuditLogs/Index", "Traceability and system hardening evidence."),
        new("System Health Check", "/System/HealthCheck/Index", "Validation scanner and QA readiness."),
        new("Printable Documents", "/Documents/Index", "Print center for client-ready operational documents.")
    ];
}

public record WorkflowLaunchItem(string Title, string Url, string Description);
