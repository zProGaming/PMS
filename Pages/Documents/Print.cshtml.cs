using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.ManagementAI;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Documents;

public class PrintModel(ApplicationDbContext context) : PageModel
{
    public string Template { get; private set; } = string.Empty;
    public string DocumentTitle { get; private set; } = "Printable Document";
    public string? MissingDataMessage { get; private set; }
    public int? SourceId { get; private set; }

    public Reservation? Reservation { get; private set; }
    public Folio? Folio { get; private set; }
    public FinanceDocument? FinanceDocument { get; private set; }
    public ARAccount? ARAccount { get; private set; }
    public Payment? Payment { get; private set; }
    public POSOrder? POSOrder { get; private set; }
    public BanquetEvent? BanquetEvent { get; private set; }
    public PurchaseRequest? PurchaseRequest { get; private set; }
    public PurchaseOrder? PurchaseOrder { get; private set; }
    public ReceivingRecord? ReceivingRecord { get; private set; }
    public StockAdjustment? StockAdjustment { get; private set; }
    public SalesAccount? SalesAccount { get; private set; }
    public ManagementDailySummary? ManagementSummary { get; private set; }
    public IList<ManagementInsight> ManagementInsights { get; private set; } = new List<ManagementInsight>();

    public async Task OnGetAsync(string template, int? id)
    {
        Template = (template ?? string.Empty).Trim().ToLowerInvariant();
        SourceId = id;
        DocumentTitle = ResolveTitle(Template);

        switch (Template)
        {
            case "reservation-confirmation":
            case "registration-card":
                Reservation = await WithOptionalId(context.Reservations
                        .Include(item => item.Guest)
                        .Include(item => item.RoomType)
                        .Include(item => item.Room)
                        .Include(item => item.RatePlan)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(Reservation is null);
                break;
            case "guest-folio":
                Folio = await WithOptionalId(context.Folios
                        .Include(item => item.Guest)
                        .Include(item => item.Reservation).ThenInclude(item => item!.Room)
                        .Include(item => item.Reservation).ThenInclude(item => item!.RoomType)
                        .Include(item => item.Items)
                        .Include(item => item.Payments)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(Folio is null);
                break;
            case "finance-document":
                FinanceDocument = await WithOptionalId(context.FinanceDocuments
                        .Include(item => item.Lines)
                        .Include(item => item.Guest)
                        .Include(item => item.Reservation)
                        .Include(item => item.SalesAccount)
                        .Include(item => item.BanquetEvent)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(FinanceDocument is null);
                break;
            case "statement-of-account":
            case "ar-aging":
                ARAccount = await WithOptionalId(context.ARAccounts
                        .Include(item => item.Invoices)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(ARAccount is null);
                break;
            case "payment-receipt":
                Payment = await WithOptionalId(context.Payments
                        .Include(item => item.Folio).ThenInclude(item => item!.Guest)
                        .Include(item => item.Folio).ThenInclude(item => item!.Reservation)
                        .Include(item => item.CashierTransactions).ThenInclude(item => item.CashierShift)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(Payment is null);
                break;
            case "pos-bill":
            case "room-charge-slip":
            case "kitchen-order-ticket":
                POSOrder = await WithOptionalId(context.POSOrders
                        .Include(item => item.Outlet)
                        .Include(item => item.DiningTable)
                        .Include(item => item.Reservation).ThenInclude(item => item!.Guest)
                        .Include(item => item.Reservation).ThenInclude(item => item!.Room)
                        .Include(item => item.Guest)
                        .Include(item => item.Items).ThenInclude(item => item.MenuItem)!.ThenInclude(item => item!.KitchenStation)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(POSOrder is null);
                break;
            case "banquet-event-order":
            case "banquet-quotation":
            case "event-contract":
                BanquetEvent = await WithOptionalId(context.BanquetEvents
                        .Include(item => item.FunctionRoom)
                        .Include(item => item.BanquetPackage)
                        .Include(item => item.BanquetEventOrder)
                        .Include(item => item.Charges)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(BanquetEvent is null);
                break;
            case "purchase-request":
                PurchaseRequest = await WithOptionalId(context.PurchaseRequests
                        .Include(item => item.Department)
                        .Include(item => item.Items).ThenInclude(item => item.InventoryItem)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(PurchaseRequest is null);
                break;
            case "purchase-order":
                PurchaseOrder = await WithOptionalId(context.PurchaseOrders
                        .Include(item => item.Supplier)
                        .Include(item => item.PurchaseRequest)
                        .Include(item => item.Items).ThenInclude(item => item.InventoryItem)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(PurchaseOrder is null);
                break;
            case "receiving-report":
                ReceivingRecord = await WithOptionalId(context.ReceivingRecords
                        .Include(item => item.Supplier)
                        .Include(item => item.PurchaseOrder)
                        .Include(item => item.Items).ThenInclude(item => item.InventoryItem)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(ReceivingRecord is null);
                break;
            case "stock-adjustment":
                StockAdjustment = await WithOptionalId(context.StockAdjustments
                        .Include(item => item.Items).ThenInclude(item => item.InventoryItem)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(StockAdjustment is null);
                break;
            case "sales-account-profile":
            case "sales-proposal":
                SalesAccount = await WithOptionalId(context.SalesAccounts
                        .Include(item => item.ContactPersons)
                        .Include(item => item.SalesLeads)
                        .Include(item => item.SalesActivities)
                        .AsNoTracking(), id)
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
                MissingIf(SalesAccount is null);
                break;
            case "management-ai-summary":
                ManagementSummary = await WithOptionalId(context.ManagementDailySummaries.AsNoTracking(), id)
                    .OrderByDescending(item => item.BusinessDate)
                    .FirstOrDefaultAsync();
                ManagementInsights = await context.ManagementInsights
                    .AsNoTracking()
                    .Where(item => !item.IsResolved)
                    .OrderByDescending(item => item.Severity)
                    .ThenByDescending(item => item.InsightDate)
                    .Take(8)
                    .ToListAsync();
                MissingIf(ManagementSummary is null);
                break;
            default:
                MissingDataMessage = "Document template not recognized.";
                break;
        }
    }

    public static int Nights(Reservation reservation) =>
        Math.Max(1, (reservation.DepartureDate.Date - reservation.ArrivalDate.Date).Days);

    public static string GuestName(Guest? guest) =>
        guest is null ? string.Empty : $"{guest.FirstName} {guest.LastName}".Trim();

    public static string Money(decimal amount) => amount.ToString("C");

    public static string AgingBucket(DateTime dueDate)
    {
        var days = (DateTime.Today - dueDate.Date).Days;
        return days <= 0 ? "Current" : days <= 30 ? "1-30 days" : days <= 60 ? "31-60 days" : days <= 90 ? "61-90 days" : "Over 90 days";
    }

    public static decimal BucketAmount(IEnumerable<ARInvoice> invoices, Func<DateTime, bool> predicate) =>
        invoices.Where(item => item.Balance > 0 && predicate(item.DueDate)).Sum(item => item.Balance);

    private void MissingIf(bool condition)
    {
        if (condition)
        {
            MissingDataMessage = "No sample record is available yet. Seed demo data from System > Demo Setup or create a record first.";
        }
    }

    private static IQueryable<T> WithOptionalId<T>(IQueryable<T> query, int? id)
        where T : class =>
        id.HasValue ? query.Where(item => EF.Property<int>(item, "Id") == id.Value) : query;

    private static string ResolveTitle(string template) => template switch
    {
        "reservation-confirmation" => "Reservation Confirmation",
        "registration-card" => "Registration Card",
        "guest-folio" => "Guest Folio",
        "finance-document" => "Finance Document",
        "statement-of-account" => "Statement of Account",
        "payment-receipt" => "Payment Receipt",
        "ar-aging" => "AR Aging Summary",
        "pos-bill" => "POS Order Bill",
        "room-charge-slip" => "Room Charge Slip",
        "kitchen-order-ticket" => "Kitchen Order Ticket",
        "banquet-event-order" => "Banquet Event Order",
        "banquet-quotation" => "Banquet Quotation",
        "event-contract" => "Event Contract Placeholder",
        "purchase-request" => "Purchase Request",
        "purchase-order" => "Purchase Order",
        "receiving-report" => "Receiving Report",
        "stock-adjustment" => "Stock Adjustment / Stock Count",
        "sales-account-profile" => "Sales Account Profile",
        "sales-proposal" => "Sales Proposal Placeholder",
        "management-ai-summary" => "Management AI Summary",
        _ => "Printable Document"
    };
}
