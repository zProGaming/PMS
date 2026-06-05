using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vantage.PMS.Pages.System.DemoWorkflowLauncher;

public class IndexModel : PageModel
{
    public IList<WorkflowLaunchItem> Items { get; } =
    [
        new("Enterprise Dashboard", "/Index", "Executive command center."),
        new("Front Office Dashboard", "/FrontOffice/Index", "Arrivals, departures, in-house, and room operations."),
        new("Room Rack", "/Housekeeping/Index", "Visual room status and housekeeping readiness."),
        new("Reservations", "/FrontOffice/Reservations/Index", "Reservation search, create, check-in, and checkout flow."),
        new("Folios", "/FrontOffice/Index", "In-house folio control for charges, payments, balance, and printout."),
        new("POS Operations", "/FoodBeverage/Index", "F&B dashboard, orders, room charge, and settlement."),
        new("Kitchen Display", "/FoodBeverageKitchen/Index", "Station-based food preparation status updates."),
        new("Banquet BEO", "/Banquet/BEOs/Index", "Banquet Event Orders and service controls."),
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
