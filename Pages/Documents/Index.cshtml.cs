using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;

namespace Vantage.PMS.Pages.Documents;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<DocumentSection> Sections { get; private set; } = new List<DocumentSection>();

    public async Task OnGetAsync()
    {
        var reservationId = await context.Reservations.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var folioId = await context.Folios.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var financeDocumentId = await context.FinanceDocuments.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var arAccountId = await context.ARAccounts.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var paymentId = await context.Payments.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var posOrderId = await context.POSOrders.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var banquetEventId = await context.BanquetEvents.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var purchaseRequestId = await context.PurchaseRequests.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var purchaseOrderId = await context.PurchaseOrders.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var receivingId = await context.ReceivingRecords.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var stockAdjustmentId = await context.StockAdjustments.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var salesAccountId = await context.SalesAccounts.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();
        var managementSummaryId = await context.ManagementDailySummaries.AsNoTracking().OrderByDescending(item => item.Id).Select(item => (int?)item.Id).FirstOrDefaultAsync();

        Sections =
        [
            new("Front Office Documents",
            [
                Link("Reservation Confirmation", "reservation-confirmation", reservationId),
                Link("Registration Card", "registration-card", reservationId),
                Link("Guest Folio", "guest-folio", folioId)
            ]),
            new("Finance Documents",
            [
                Link("Finance Document", "finance-document", financeDocumentId),
                Link("Statement of Account", "statement-of-account", arAccountId),
                Link("Payment Receipt", "payment-receipt", paymentId),
                Link("AR Aging Summary", "ar-aging", arAccountId)
            ]),
            new("F&B Documents",
            [
                Link("POS Bill", "pos-bill", posOrderId),
                Link("Room Charge Slip", "room-charge-slip", posOrderId),
                Link("Kitchen Order Ticket", "kitchen-order-ticket", posOrderId)
            ]),
            new("Banquet Documents",
            [
                Link("Banquet Event Order", "banquet-event-order", banquetEventId),
                Link("Banquet Quotation", "banquet-quotation", banquetEventId),
                Link("Event Contract Placeholder", "event-contract", banquetEventId)
            ]),
            new("Inventory and Purchasing Documents",
            [
                Link("Purchase Request", "purchase-request", purchaseRequestId),
                Link("Purchase Order", "purchase-order", purchaseOrderId),
                Link("Receiving Report", "receiving-report", receivingId),
                Link("Stock Adjustment / Stock Count", "stock-adjustment", stockAdjustmentId)
            ]),
            new("Sales Documents",
            [
                Link("Sales Account Profile", "sales-account-profile", salesAccountId),
                Link("Sales Proposal Placeholder", "sales-proposal", salesAccountId)
            ]),
            new("Management Documents",
            [
                Link("Management AI Summary", "management-ai-summary", managementSummaryId),
                Link("Client Demo Package", "client-demo-package", null, "/System/ClientDemoPackage/Index")
            ])
        ];
    }

    private static DocumentLink Link(string title, string template, int? id, string? page = null) =>
        new(title, template, id, page, id.HasValue || page is not null);
}

public record DocumentSection(string Title, IList<DocumentLink> Links);

public record DocumentLink(string Title, string Template, int? SampleId, string? Page, bool IsAvailable);
