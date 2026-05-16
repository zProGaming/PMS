using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.ManagementAI;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public class SystemNotificationService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task CreateAsync(
        string title,
        string message,
        SystemNotificationType type,
        SystemSeverity severity,
        string? targetRole,
        string? targetUserId,
        string relatedModule,
        string relatedReferenceType,
        int? relatedReferenceId)
    {
        var duplicateExists = await _context.SystemNotifications.AnyAsync(notification =>
            !notification.IsRead &&
            notification.Title == title &&
            notification.TargetRole == targetRole &&
            notification.TargetUserId == targetUserId &&
            notification.RelatedModule == relatedModule &&
            notification.RelatedReferenceType == relatedReferenceType &&
            notification.RelatedReferenceId == relatedReferenceId);

        if (duplicateExists)
        {
            return;
        }

        _context.SystemNotifications.Add(new SystemNotification
        {
            Title = title,
            Message = message,
            NotificationType = type,
            Severity = severity,
            TargetRole = targetRole,
            TargetUserId = targetUserId,
            RelatedModule = relatedModule,
            RelatedReferenceType = relatedReferenceType,
            RelatedReferenceId = relatedReferenceId,
            CreatedAt = DateTime.Now
        });
    }

    public async Task<int> GenerateOperationalNotificationsAsync()
    {
        var createdBefore = _context.SystemNotifications.Local.Count;

        var pendingApprovals = await _context.VoidRequests.CountAsync(request => request.Status == ApprovalStatus.Pending)
            + await _context.DiscountApprovals.CountAsync(discount => discount.Status == ApprovalStatus.Pending)
            + await _context.RefundTransactions.CountAsync(refund => refund.Status == RefundStatus.Requested || refund.Status == RefundStatus.ForApproval);
        if (pendingApprovals > 0)
        {
            await CreateAsync("Pending finance approvals", $"{pendingApprovals} finance approval item(s) need review.", SystemNotificationType.Approval, SystemSeverity.Medium, PmsRoles.FinanceManager, null, "Finance", "ApprovalQueue", null);
        }

        var urgentGuestRequests = await _context.GuestServiceRequests.CountAsync(request =>
            request.Priority == GuestServiceRequestPriority.Urgent &&
            request.Status != GuestServiceRequestStatus.Completed &&
            request.Status != GuestServiceRequestStatus.Cancelled);
        if (urgentGuestRequests > 0)
        {
            await CreateAsync("Urgent guest service requests", $"{urgentGuestRequests} urgent guest request(s) are open.", SystemNotificationType.Task, SystemSeverity.High, PmsRoles.FrontOfficeManager, null, "Guest Portal", "GuestServiceRequests", null);
        }

        var lowStockItems = await _context.InventoryItems.CountAsync(item => item.IsActive && item.CurrentStock <= item.ReorderLevel);
        if (lowStockItems > 0)
        {
            await CreateAsync("Low stock inventory alert", $"{lowStockItems} inventory item(s) are at or below reorder level.", SystemNotificationType.Warning, SystemSeverity.Medium, PmsRoles.InventoryManager, null, "Inventory", "InventoryItems", null);
        }

        var today = DateTime.Today;
        var overdueArInvoices = await _context.ARInvoices.CountAsync(invoice =>
            invoice.DueDate < today &&
            invoice.Balance > 0 &&
            invoice.Status != ARInvoiceStatus.Paid &&
            invoice.Status != ARInvoiceStatus.Cancelled &&
            invoice.Status != ARInvoiceStatus.WrittenOff);
        if (overdueArInvoices > 0)
        {
            await CreateAsync("Overdue AR invoices", $"{overdueArInvoices} AR invoice(s) are overdue.", SystemNotificationType.Warning, SystemSeverity.High, PmsRoles.FinanceManager, null, "Accounts Receivable", "ARInvoices", null);
        }

        var highBalanceFolios = await CountHighBalanceFoliosAsync(50000);
        if (highBalanceFolios > 0)
        {
            await CreateAsync("High balance folios", $"{highBalanceFolios} guest folio(s) have balances above the warning threshold.", SystemNotificationType.Warning, SystemSeverity.High, PmsRoles.FrontOfficeManager, null, "Front Office", "Folios", null);
        }

        var criticalInsights = await _context.ManagementInsights.CountAsync(insight =>
            !insight.IsResolved &&
            insight.Severity == ManagementInsightSeverity.Critical);
        if (criticalInsights > 0)
        {
            await CreateAsync("Critical management AI insights", $"{criticalInsights} critical management insight(s) require attention.", SystemNotificationType.Warning, SystemSeverity.Critical, PmsRoles.GeneralManager, null, "Management AI", "ManagementInsights", null);
        }

        var createdAfter = _context.SystemNotifications.Local.Count;
        await _context.SaveChangesAsync();
        return Math.Max(0, createdAfter - createdBefore);
    }

    public async Task MarkReadAsync(int id, string? userId, IEnumerable<string> userRoles)
    {
        var notification = await _context.SystemNotifications.FindAsync(id);
        if (notification is null || !CanView(notification, userId, userRoles))
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public static bool CanView(SystemNotification notification, string? userId, IEnumerable<string> userRoles)
    {
        if (!string.IsNullOrWhiteSpace(notification.TargetUserId) && notification.TargetUserId == userId)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(notification.TargetRole) && userRoles.Contains(notification.TargetRole))
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(notification.TargetUserId) && string.IsNullOrWhiteSpace(notification.TargetRole);
    }

    private async Task<int> CountHighBalanceFoliosAsync(decimal threshold)
    {
        var balances = await _context.Folios
            .Select(folio => new
            {
                Balance = (_context.FolioItems
                    .Where(item => item.FolioId == folio.Id && !item.IsVoided)
                    .Sum(item => (decimal?)item.Amount) ?? 0) -
                    (_context.Payments
                        .Where(payment =>
                            payment.FolioId == folio.Id &&
                            payment.Status != PaymentStatus.Voided &&
                            payment.Status != PaymentStatus.Failed)
                        .Sum(payment => (decimal?)payment.Amount) ?? 0)
            })
            .ToListAsync();

        return balances.Count(item => item.Balance > threshold);
    }
}
