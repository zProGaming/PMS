using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Banquet;
using Vantage.PMS.Models.Executive;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Models.Housekeeping;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Models.ManagementAI;

namespace Vantage.PMS.Services;

public class ManagementInsightService(ApplicationDbContext context, CashFlowReportService cashFlowReportService)
{
    private readonly ApplicationDbContext _context = context;
    private static readonly ReservationStatus[] SoldReservationStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Reserved,
        ReservationStatus.CheckedIn
    ];

    public async Task<IReadOnlyList<ManagementInsight>> GenerateInsightsForBusinessDateAsync(ManagementDailySummary summary, string generatedBy)
    {
        var createdInsights = new List<ManagementInsight>();
        var businessDate = summary.BusinessDate.Date;
        var nextBusinessDate = businessDate.AddDays(1);
        var now = DateTime.Now;

        if (summary.OccupancyPercentage > 90)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Opportunity, ManagementInsightSeverity.High, "Occupancy above 90%", $"Occupancy is {summary.OccupancyPercentage:0.#}% for the business date.", "Review remaining inventory, rate fences, and upgrade opportunities before releasing additional rooms.", "Front Office", "Occupancy", null, generatedBy);
        }

        if (summary.ArrivalsToday > 20)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Operational, ManagementInsightSeverity.Medium, "High arrival volume today", $"{summary.ArrivalsToday} arrivals are expected today.", "Stage check-in documents and schedule additional front desk coverage for peak arrival windows.", "Front Office", "Reservations", null, generatedBy);
        }

        if (summary.DeparturesToday > 20)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Housekeeping, ManagementInsightSeverity.Medium, "High departure volume today", $"{summary.DeparturesToday} departures are expected today.", "Prioritize due-out rooms for housekeeping assignment and front desk settlement follow-up.", "Housekeeping", "Reservations", null, generatedBy);
        }

        var noShowsToday = await _context.Reservations.CountAsync(reservation =>
            reservation.Status == ReservationStatus.NoShow &&
            reservation.ArrivalDate >= businessDate &&
            reservation.ArrivalDate < nextBusinessDate);
        if (noShowsToday > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Revenue, ManagementInsightSeverity.Medium, "No-show reservations today", $"{noShowsToday} reservation(s) are marked no-show today.", "Review no-show charge policy and release unsold inventory where appropriate.", "Front Office", "Reservations", null, generatedBy);
        }

        if (summary.TotalRooms > 0 && summary.DirtyRooms > summary.TotalRooms * 0.2m)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Housekeeping, ManagementInsightSeverity.High, "Dirty rooms above 20% of inventory", $"{summary.DirtyRooms} of {summary.TotalRooms} room(s) are dirty.", "Assign additional room attendants and prioritize rooms needed for arrivals.", "Housekeeping", "Rooms", null, generatedBy);
        }

        if (summary.TotalRooms > 0 && summary.OutOfOrderRooms > summary.TotalRooms * 0.05m)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Maintenance, ManagementInsightSeverity.High, "Out of order rooms above 5%", $"{summary.OutOfOrderRooms} of {summary.TotalRooms} room(s) are out of order.", "Review maintenance blockers and return sellable rooms to inventory where possible.", "Maintenance", "Rooms", null, generatedBy);
        }

        if (summary.PendingHousekeepingTasks > 10)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Housekeeping, ManagementInsightSeverity.Medium, "Pending housekeeping task backlog", $"{summary.PendingHousekeepingTasks} housekeeping task(s) remain open.", "Reassign open tasks by priority and focus on guest-impacting rooms first.", "Housekeeping", "HousekeepingTasks", null, generatedBy);
        }

        if (summary.OutstandingGuestBalances > 50000)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.High, "Outstanding guest balances above threshold", $"Outstanding guest balances total {summary.OutstandingGuestBalances:C}.", "Review high-balance guest folios before allowing checkout.", "Finance", "Folios", null, generatedBy);
        }

        var openPreviousShifts = await _context.CashierShifts.CountAsync(shift =>
            shift.Status == CashierShiftStatus.Open &&
            shift.BusinessDate < businessDate);
        if (openPreviousShifts > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Critical, "Open cashier shifts from previous business date", $"{openPreviousShifts} cashier shift(s) from a previous business date are still open.", "Close or audit prior business date cashier shifts before continuing finance close procedures.", "Finance", "CashierShifts", null, generatedBy);
        }

        var pendingVoidApprovals = await _context.VoidRequests.CountAsync(request => request.Status == ApprovalStatus.Pending);
        if (pendingVoidApprovals > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "Pending void approvals", $"{pendingVoidApprovals} void request(s) require approval.", "Review pending void requests and preserve audit notes before night audit.", "Finance", "VoidRequests", null, generatedBy);
        }

        var pendingRefundApprovals = await _context.RefundTransactions.CountAsync(refund =>
            refund.Status == RefundStatus.Requested ||
            refund.Status == RefundStatus.ForApproval);
        if (pendingRefundApprovals > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "Pending refund approvals", $"{pendingRefundApprovals} refund request(s) require approval.", "Approve, reject, or process refunds before cashier reconciliation.", "Finance", "Refunds", null, generatedBy);
        }

        var pendingDiscountApprovals = await _context.DiscountApprovals.CountAsync(discount => discount.Status == ApprovalStatus.Pending);
        if (pendingDiscountApprovals > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "Pending discount approvals", $"{pendingDiscountApprovals} discount request(s) require approval.", "Review requested discounts and confirm they do not create negative balances.", "Finance", "DiscountApprovals", null, generatedBy);
        }

        if (summary.FBRevenue == 0 && summary.OccupancyPercentage > 50)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Opportunity, ManagementInsightSeverity.Medium, "F&B revenue is zero despite occupancy", $"Occupancy is {summary.OccupancyPercentage:0.#}% but no F&B revenue is posted today.", "Promote restaurant, bar, cafe, and room service offers to in-house guests.", "F&B Service", "POSOrders", null, generatedBy);
        }

        var openOldOrders = await _context.POSOrders.CountAsync(order =>
            order.OrderStatus != POSOrderStatus.Closed &&
            order.OrderStatus != POSOrderStatus.Cancelled &&
            order.OrderDate <= now.AddHours(-2));
        if (openOldOrders > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Operational, ManagementInsightSeverity.Medium, "Open POS orders older than 2 hours", $"{openOldOrders} POS order(s) have been open for more than 2 hours.", "Review open checks and close or escalate stale F&B orders.", "F&B Service", "POSOrders", null, generatedBy);
        }

        var delayedKitchenItems = await _context.POSOrderItems.CountAsync(item =>
            item.SentToKitchenAt != null &&
            item.SentToKitchenAt <= now.AddMinutes(-20) &&
            item.ItemStatus != POSOrderItemStatus.Ready &&
            item.ItemStatus != POSOrderItemStatus.Served &&
            item.ItemStatus != POSOrderItemStatus.Cancelled &&
            item.ItemStatus != POSOrderItemStatus.Voided);
        if (delayedKitchenItems > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Operational, ManagementInsightSeverity.Medium, "Delayed kitchen items", $"{delayedKitchenItems} kitchen item(s) have exceeded the 20-minute delay threshold.", "Expedite delayed kitchen items and update guests or servers on expected timing.", "F&B Kitchen", "POSOrderItems", null, generatedBy);
        }

        var banquetEventsToday = await _context.BanquetEvents.CountAsync(banquetEvent =>
            banquetEvent.EventDate >= businessDate &&
            banquetEvent.EventDate < nextBusinessDate &&
            banquetEvent.EventStatus != BanquetEventStatus.Cancelled &&
            banquetEvent.EventStatus != BanquetEventStatus.Lost);
        if (banquetEventsToday > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Banquet, ManagementInsightSeverity.Info, "Banquet events today", $"{banquetEventsToday} banquet event(s) are scheduled today.", "Confirm BEO distribution, setup readiness, and kitchen timing for today's events.", "Banquet", "BanquetEvents", null, generatedBy);
        }

        var confirmedBanquetRevenueThisMonth = await CalculateConfirmedBanquetRevenueThisMonthAsync(businessDate);
        if (confirmedBanquetRevenueThisMonth > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Revenue, ManagementInsightSeverity.Info, "Confirmed banquet revenue this month", $"Confirmed banquet revenue projection is {confirmedBanquetRevenueThisMonth:C}.", "Track confirmed banquet revenue against monthly forecast and staffing plans.", "Banquet", "BanquetEvents", null, generatedBy);
        }

        var confirmedEventsWithoutBeo = await _context.BanquetEvents.CountAsync(banquetEvent =>
            banquetEvent.EventStatus == BanquetEventStatus.Confirmed &&
            banquetEvent.BanquetEventOrder == null);
        if (confirmedEventsWithoutBeo > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Banquet, ManagementInsightSeverity.High, "Confirmed banquet events without BEO", $"{confirmedEventsWithoutBeo} confirmed banquet event(s) do not have a BEO.", "Prepare banquet BEOs for confirmed events without approved instructions.", "Banquet", "BanquetEventOrders", null, generatedBy);
        }

        var nextSevenDayOccupancy = await CalculateNextSevenDayOccupancyAsync(businessDate, summary.TotalRooms);
        if (nextSevenDayOccupancy < 40)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Revenue, ManagementInsightSeverity.Medium, "Next 7 days occupancy below 40%", $"Projected occupancy for the next 7 days is {nextSevenDayOccupancy:0.#}%.", "Review low-demand dates, promotions, and corporate account pickup.", "Revenue", "Reservations", null, generatedBy);
        }

        var hasStopSellBelow80 = nextSevenDayOccupancy < 80 && await HasStopSellInNextSevenDaysAsync(businessDate);
        if (hasStopSellBelow80)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Revenue, ManagementInsightSeverity.Medium, "Stop sell active while occupancy is below 80%", "A stop sell restriction or inventory control is active while forward occupancy is below 80%.", "Review rate restrictions for low-occupancy dates before blocking additional bookings.", "Revenue", "RateRestrictions", null, generatedBy);
        }

        var activeRoomTypesMissingRates = await CountActiveRoomTypesMissingRatesAsync();
        if (activeRoomTypesMissingRates > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Revenue, ManagementInsightSeverity.High, "Active room types missing active rate setup", $"{activeRoomTypesMissingRates} active room type(s) do not have active rate setup.", "Create or activate rate plans and room type rates before opening online or front office selling.", "Revenue", "RatePlans", null, generatedBy);
        }

        var urgentGuestRequests = await _context.GuestServiceRequests.CountAsync(request =>
            request.Priority == GuestServiceRequestPriority.Urgent &&
            request.Status != GuestServiceRequestStatus.Completed &&
            request.Status != GuestServiceRequestStatus.Cancelled);
        if (urgentGuestRequests > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.GuestExperience, ManagementInsightSeverity.High, "Open urgent guest service requests", $"{urgentGuestRequests} urgent guest service request(s) remain open.", "Assign urgent requests immediately and confirm resolution with the guest.", "Guest Portal", "GuestServiceRequests", null, generatedBy);
        }

        var negativeFeedback = await _context.GuestFeedbacks.CountAsync(feedback =>
            !feedback.IsResolved &&
            feedback.Rating <= 2);
        if (negativeFeedback > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.GuestExperience, ManagementInsightSeverity.High, "Negative guest feedback unresolved", $"{negativeFeedback} feedback item(s) with rating 2 or below are unresolved.", "Contact the guest where possible and record recovery actions.", "Guest Portal", "GuestFeedback", null, generatedBy);
        }

        var pendingExpressCheckout = await _context.ExpressCheckoutRequests.CountAsync(request =>
            request.Status == ExpressCheckoutRequestStatus.Requested ||
            request.Status == ExpressCheckoutRequestStatus.UnderReview);
        if (pendingExpressCheckout > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Operational, ManagementInsightSeverity.Medium, "Pending express checkout requests", $"{pendingExpressCheckout} express checkout request(s) are pending.", "Verify folio balances and complete eligible express checkout requests.", "Front Office", "ExpressCheckoutRequests", null, generatedBy);
        }

        if (summary.LowStockItems > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Inventory, ManagementInsightSeverity.Medium, "Low stock inventory items", $"{summary.LowStockItems} active inventory item(s) are at or below reorder level.", "Reorder low-stock items before they affect F&B or housekeeping operations.", "Inventory", "InventoryItems", null, generatedBy);
        }

        var outOfStockItems = await _context.InventoryItems.CountAsync(item => item.IsActive && item.CurrentStock <= 0);
        if (outOfStockItems > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Inventory, ManagementInsightSeverity.High, "Active inventory items out of stock", $"{outOfStockItems} active inventory item(s) are out of stock.", "Prioritize purchasing or transfer actions for out-of-stock operational supplies.", "Inventory", "InventoryItems", null, generatedBy);
        }

        var expiringItems = await _context.InventoryItems.CountAsync(item =>
            item.IsActive &&
            item.IsPerishable &&
            item.ExpiryDate != null &&
            item.ExpiryDate >= businessDate &&
            item.ExpiryDate < businessDate.AddDays(8));
        if (expiringItems > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Inventory, ManagementInsightSeverity.Medium, "Perishable items expiring within 7 days", $"{expiringItems} perishable inventory item(s) expire within 7 days.", "Review expiring stock for usage, transfer, or wastage controls.", "Inventory", "InventoryItems", null, generatedBy);
        }

        if (summary.PendingPurchaseRequests > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Inventory, ManagementInsightSeverity.Info, "Pending purchase requests", $"{summary.PendingPurchaseRequests} purchase request(s) are pending approval.", "Review pending purchase requests and convert approved needs to purchase orders.", "Purchasing", "PurchaseRequests", null, generatedBy);
        }

        var arOverdueBalance = await _context.ARInvoices
            .Where(invoice =>
                invoice.DueDate < businessDate &&
                invoice.Balance > 0 &&
                invoice.Status != ARInvoiceStatus.Cancelled &&
                invoice.Status != ARInvoiceStatus.WrittenOff)
            .SumAsync(invoice => (decimal?)invoice.Balance) ?? 0;
        if (arOverdueBalance > 100000)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.High, "AR overdue balance above threshold", $"Overdue AR balance is {arOverdueBalance:C}.", "Follow up AR accounts with overdue balances and prioritize large balances.", "Accounts Receivable", "ARInvoices", null, generatedBy);
        }

        var arOver90Invoices = await _context.ARInvoices.CountAsync(invoice =>
            invoice.DueDate < businessDate.AddDays(-90) &&
            invoice.Balance > 0 &&
            invoice.Status != ARInvoiceStatus.Cancelled &&
            invoice.Status != ARInvoiceStatus.WrittenOff);
        if (arOver90Invoices > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Critical, "AR invoices over 90 days past due", $"{arOver90Invoices} AR invoice(s) are more than 90 days past due.", "Escalate over-90-day AR invoices for collection review or write-off decision.", "Accounts Receivable", "ARInvoices", null, generatedBy);
        }

        var unpostedFinanceTransactions = await _context.FolioItems.CountAsync(item =>
                !item.IsVoided &&
                !_context.JournalEntries.Any(entry =>
                    entry.Status == JournalEntryStatus.Posted &&
                    entry.SourceReferenceId == item.Id &&
                    (entry.SourceTransactionType == SourceTransactionType.FolioCharge ||
                        entry.SourceTransactionType == SourceTransactionType.RoomCharge)))
            + await _context.Payments.CountAsync(payment =>
                payment.Status == PaymentStatus.Completed &&
                !_context.JournalEntries.Any(entry =>
                    entry.Status == JournalEntryStatus.Posted &&
                    entry.SourceReferenceId == payment.Id &&
                    entry.SourceTransactionType == SourceTransactionType.FolioPayment));
        if (unpostedFinanceTransactions > 25)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "Unposted accounting transactions exceed threshold", $"{unpostedFinanceTransactions} finance source transaction(s) are not posted to the general ledger.", "Create and process posting batches before month-end close.", "Accounting", "PostingBatches", null, generatedBy);
        }

        var trialBalance = await _context.JournalEntryLines
            .Where(line => line.JournalEntry != null && line.JournalEntry.Status == JournalEntryStatus.Posted)
            .GroupBy(_ => 1)
            .Select(group => new { Debit = group.Sum(line => line.DebitAmount), Credit = group.Sum(line => line.CreditAmount) })
            .FirstOrDefaultAsync();
        if ((trialBalance?.Debit ?? 0) != (trialBalance?.Credit ?? 0))
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Critical, "Trial balance is out of balance", "Posted journal entry totals do not balance.", "Review posted journals and reverse or correct invalid entries before issuing financial reports.", "Accounting", "TrialBalance", null, generatedBy);
        }

        var priorOpenPeriods = await _context.AccountingPeriods.CountAsync(period =>
            period.Status == AccountingPeriodStatus.Open &&
            period.EndDate < businessDate.AddDays(-7));
        if (priorOpenPeriods > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "Accounting period remains open past month-end", $"{priorOpenPeriods} prior accounting period(s) remain open.", "Review pending posting batches and close accounting periods after finance review.", "Accounting", "AccountingPeriods", null, generatedBy);
        }

        var monthStart = new DateTime(businessDate.Year, businessDate.Month, 1);
        var cashFlow = await cashFlowReportService.GenerateStatementAsync(monthStart, businessDate);
        if (cashFlow.NetCashFromOperatingActivities < 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.High, "Negative operating cash flow month-to-date", $"Operating cash flow is {cashFlow.NetCashFromOperatingActivities:C} for the month to date.", "Review collections, AP disbursement timing, and unmapped cash movements before final reporting.", "Accounting", "StatementOfCashFlows", null, generatedBy);
        }

        if (cashFlow.EndingCashBalance < 50000 && cashFlow.EndingCashBalance != 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.High, "Ending cash below management threshold", $"Ending cash is {cashFlow.EndingCashBalance:C}.", "Review cash position, pending bank reconciliations, and upcoming disbursements.", "Accounting", "StatementOfCashFlows", null, generatedBy);
        }

        if (cashFlow.UnmappedItemCount > 10)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "High unmapped cash flow items", $"{cashFlow.UnmappedItemCount} cash movement item(s) need classification review.", "Create cash flow mapping rules so operating, investing, and financing sections are reliable.", "Accounting", "CashFlowMappingRules", null, generatedBy);
        }

        if (cashFlow.NetCashFromFinancingActivities < -100000)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "Large financing cash outflow", $"Financing cash flow is {cashFlow.NetCashFromFinancingActivities:C} month-to-date.", "Review loan repayments, owner withdrawals, and financing classifications.", "Accounting", "StatementOfCashFlows", null, generatedBy);
        }

        if (cashFlow.UnreconciledBankTransactionCount > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "Bank reconciliation pending for cash flow period", $"{cashFlow.UnreconciledBankTransactionCount} unreconciled bank transaction(s) exist in the cash flow period.", "Review bank reconciliation before relying on final cash flow reporting.", "Banking", "BankReconciliations", null, generatedBy);
        }

        if (cashFlow.ReconciliationDifference != 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.High, "Cash flow reconciliation difference", $"Cash flow reconciliation difference is {cashFlow.ReconciliationDifference:C}.", "Review cash account settings, cash-to-cash transfers, and mapping rules.", "Accounting", "StatementOfCashFlows", null, generatedBy);
        }

        var payrollApprovedNotPosted = await _context.PayrollPeriods.CountAsync(period => period.Status == PayrollPeriodStatus.Approved);
        if (payrollApprovedNotPosted > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "Approved payroll periods not posted", $"{payrollApprovedNotPosted} approved payroll period(s) are not yet posted to the general ledger.", "Post approved payroll cost before issuing department P&L or USALI-style management reports.", "Labor Costing", "PayrollPeriods", null, generatedBy);
        }

        var serviceChargePendingApproval = await _context.ServiceChargePools.CountAsync(pool => pool.Status == ServiceChargePoolStatus.ForApproval);
        if (serviceChargePendingApproval > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "Service charge pool pending approval", $"{serviceChargePendingApproval} service charge pool(s) require approval.", "Review service charge distribution before payroll close or month-end reporting.", "Labor Costing", "ServiceChargePools", null, generatedBy);
        }

        var nextMonth = monthStart.AddMonths(1);
        var laborActuals = await _context.PayrollCostEntries
            .Where(entry => entry.DepartmentId != null &&
                entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate < nextMonth &&
                entry.PayrollPeriod.EndDate >= monthStart &&
                entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled)
            .GroupBy(entry => entry.DepartmentId)
            .Select(group => new { DepartmentId = group.Key, Cost = group.Sum(entry => entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay) })
            .ToListAsync();
        var budgets = await _context.DepartmentLaborBudgets
            .Where(budget => budget.Month == businessDate.Month && budget.Year == businessDate.Year && budget.BudgetedLaborCost > 0)
            .ToListAsync();
        var departmentsOverBudget = budgets.Count(budget =>
            laborActuals.Where(actual => actual.DepartmentId == budget.DepartmentId).Sum(actual => actual.Cost) > budget.BudgetedLaborCost * 1.10m);
        if (departmentsOverBudget > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.High, "Labor cost exceeds budget by more than 10%", $"{departmentsOverBudget} department(s) are above labor budget by more than 10%.", "Review staffing, overtime, and schedule assumptions by department before the next payroll close.", "Labor Costing", "DepartmentLaborBudgets", null, generatedBy);
        }

        var payrollWithoutUsali = await _context.PayrollCostEntries.CountAsync(entry =>
            entry.USALIDepartmentId == null &&
            (entry.EmployeeCostProfile == null || entry.EmployeeCostProfile.USALIDepartmentId == null));
        if (payrollWithoutUsali > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.Medium, "Payroll cost missing USALI mapping", $"{payrollWithoutUsali} payroll cost entrie(s) are missing USALI department mapping.", "Complete employee or payroll entry USALI mappings so labor flows correctly to management reports.", "Labor Costing", "PayrollCostEntries", null, generatedBy);
        }

        var criticalExecutiveAlerts = await _context.ExecutiveAlerts.CountAsync(alert =>
            !alert.IsResolved &&
            (alert.Severity == KPIStatus.Critical || alert.Severity == KPIStatus.Warning));
        if (criticalExecutiveAlerts > 0)
        {
            await AddInsightAsync(createdInsights, businessDate, ManagementInsightType.Financial, ManagementInsightSeverity.High, "Executive alerts require management review", $"{criticalExecutiveAlerts} unresolved executive alert(s) are warning or critical.", "Open the Executive Dashboard, resolve high-risk alerts, and generate an updated executive snapshot.", "Executive", "ExecutiveAlerts", null, generatedBy);
        }

        await _context.SaveChangesAsync();
        return createdInsights;
    }

    public async Task<bool> ResolveInsightAsync(int insightId, string resolvedBy)
    {
        var insight = await _context.ManagementInsights.FindAsync(insightId);
        if (insight is null)
        {
            return false;
        }

        insight.IsResolved = true;
        insight.ResolvedBy = resolvedBy;
        insight.ResolvedAt = DateTime.Now;
        _context.AIActionLogs.Add(new AIActionLog
        {
            ActionDate = DateTime.Now,
            ActionType = AIActionType.InsightResolved,
            Module = insight.RelatedModule,
            Description = $"Resolved insight: {insight.Title}",
            PerformedBy = resolvedBy,
            RelatedInsightId = insight.Id
        });

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task AddInsightAsync(
        ICollection<ManagementInsight> createdInsights,
        DateTime insightDate,
        ManagementInsightType insightType,
        ManagementInsightSeverity severity,
        string title,
        string summary,
        string recommendation,
        string relatedModule,
        string relatedReferenceType,
        int? relatedReferenceId,
        string generatedBy)
    {
        var duplicateExists = await _context.ManagementInsights.AnyAsync(insight =>
            !insight.IsResolved &&
            insight.InsightDate == insightDate &&
            insight.Title == title &&
            insight.RelatedModule == relatedModule &&
            insight.RelatedReferenceType == relatedReferenceType &&
            insight.RelatedReferenceId == relatedReferenceId);

        if (duplicateExists)
        {
            return;
        }

        var insight = new ManagementInsight
        {
            InsightDate = insightDate,
            InsightType = insightType,
            Severity = severity,
            Title = title,
            Summary = summary,
            Recommendation = recommendation,
            RelatedModule = relatedModule,
            RelatedReferenceType = relatedReferenceType,
            RelatedReferenceId = relatedReferenceId,
            CreatedAt = DateTime.Now
        };

        _context.ManagementInsights.Add(insight);
        createdInsights.Add(insight);
        _context.AIActionLogs.Add(new AIActionLog
        {
            ActionDate = DateTime.Now,
            ActionType = AIActionType.InsightGenerated,
            Module = relatedModule,
            Description = $"Generated insight: {title}",
            PerformedBy = generatedBy,
            RelatedInsight = insight
        });
    }

    private async Task<decimal> CalculateNextSevenDayOccupancyAsync(DateTime businessDate, int totalRooms)
    {
        if (totalRooms <= 0)
        {
            return 0;
        }

        var windowEnd = businessDate.AddDays(7);
        var reservations = await _context.Reservations
            .AsNoTracking()
            .Where(reservation =>
                SoldReservationStatuses.Contains(reservation.Status) &&
                reservation.ArrivalDate < windowEnd &&
                reservation.DepartureDate > businessDate)
            .Select(reservation => new
            {
                reservation.ArrivalDate,
                reservation.DepartureDate
            })
            .ToListAsync();

        var soldRoomNights = reservations.Sum(reservation =>
            CountOverlapNights(reservation.ArrivalDate.Date, reservation.DepartureDate.Date, businessDate, windowEnd));

        return Percent(soldRoomNights, totalRooms * 7);
    }

    private async Task<bool> HasStopSellInNextSevenDaysAsync(DateTime businessDate)
    {
        var windowEnd = businessDate.AddDays(7);
        var hasRateStopSell = await _context.RateRestrictions.AnyAsync(restriction =>
            restriction.StopSell &&
            restriction.RestrictionDate >= businessDate &&
            restriction.RestrictionDate < windowEnd);

        if (hasRateStopSell)
        {
            return true;
        }

        return await _context.RoomInventoryControls.AnyAsync(control =>
            control.StopSell &&
            control.InventoryDate >= businessDate &&
            control.InventoryDate < windowEnd);
    }

    private async Task<int> CountActiveRoomTypesMissingRatesAsync()
    {
        var activeRatePlanExists = await _context.RatePlans.AnyAsync(ratePlan => ratePlan.IsActive);
        if (!activeRatePlanExists)
        {
            return await _context.RoomTypes.CountAsync(roomType => roomType.IsActive);
        }

        return await _context.RoomTypes.CountAsync(roomType =>
            roomType.IsActive &&
            !_context.RoomTypeRates.Any(rate => rate.RoomTypeId == roomType.Id && rate.IsActive));
    }

    private async Task<decimal> CalculateConfirmedBanquetRevenueThisMonthAsync(DateTime businessDate)
    {
        var monthStart = new DateTime(businessDate.Year, businessDate.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var confirmedEvents = await _context.BanquetEvents
            .AsNoTracking()
            .Include(banquetEvent => banquetEvent.BanquetPackage)
            .Include(banquetEvent => banquetEvent.Charges)
            .Where(banquetEvent =>
                banquetEvent.EventStatus == BanquetEventStatus.Confirmed &&
                banquetEvent.EventDate >= monthStart &&
                banquetEvent.EventDate < nextMonth)
            .ToListAsync();

        return confirmedEvents.Sum(banquetEvent =>
            banquetEvent.Charges.Where(charge => !charge.IsVoided).Sum(charge => charge.Amount) +
            (banquetEvent.BanquetPackage?.PricePerPax ?? 0) * banquetEvent.GuaranteedPax);
    }

    private static int CountOverlapNights(DateTime arrival, DateTime departure, DateTime windowStart, DateTime windowEnd)
    {
        var start = arrival > windowStart ? arrival : windowStart;
        var end = departure < windowEnd ? departure : windowEnd;
        return Math.Max(0, (end - start).Days);
    }

    private static decimal Percent(decimal numerator, decimal denominator)
    {
        return denominator <= 0 ? 0 : numerator / denominator * 100;
    }
}
