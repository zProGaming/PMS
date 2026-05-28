using System.Security.Claims;
using Vantage.PMS.Authorization;
using Vantage.PMS.Models.Reports;

namespace Vantage.PMS.Services;

public class ReportCatalogService
{
    private readonly bool _showPlannedReports;

    public ReportCatalogService(IConfiguration configuration)
    {
        _showPlannedReports = configuration.GetValue<bool>("Reports:ShowPlannedReports");
    }

    public IReadOnlyList<ReportCatalogEntry> GetCatalog() => GetVisibleCatalog();

    public IReadOnlyList<ReportCatalogEntry> GetAuthorizedCatalog(ClaimsPrincipal user)
    {
        if (user.IsInRole(PmsRoles.SystemAdmin))
        {
            return GetVisibleCatalog();
        }

        return GetVisibleCatalog()
            .Where(item => item.RequiredRoles.Length == 0 || item.RequiredRoles.Any(user.IsInRole))
            .ToList();
    }

    public ReportCatalogEntry? Find(string reportKey)
    {
        return DefaultCatalog.FirstOrDefault(item => item.ReportKey.Equals(reportKey, StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyList<ReportCatalogEntry> GetVisibleCatalog()
    {
        return _showPlannedReports
            ? DefaultCatalog
            : DefaultCatalog.Where(item => item.IsAvailable).ToList();
    }

    public static string FormatCategory(ReportCategory category)
    {
        return category switch
        {
            ReportCategory.FrontOffice => "Front Office",
            ReportCategory.PhilippineFinance => "Philippine Finance",
            ReportCategory.AccountsReceivable => "Accounts Receivable",
            ReportCategory.AccountsPayable => "Accounts Payable",
            ReportCategory.PayrollLabor => "Payroll and Labor",
            ReportCategory.FoodBeverage => "F&B",
            ReportCategory.BookingEngine => "Booking Engine",
            ReportCategory.GuestPortal => "Guest Portal",
            ReportCategory.ManagementAI => "Management AI",
            ReportCategory.AuditSystem => "Audit and System",
            _ => category.ToString()
        };
    }

    private static readonly string[] OperationalRoles =
    [
        PmsRoles.SystemAdmin,
        PmsRoles.GeneralManager,
        PmsRoles.FrontOfficeManager,
        PmsRoles.FrontDesk
    ];

    private static readonly string[] HousekeepingRoles =
    [
        PmsRoles.SystemAdmin,
        PmsRoles.GeneralManager,
        PmsRoles.HousekeepingSupervisor
    ];

    private static readonly string[] FinanceRoles = PmsRoles.FinanceApprovals;
    private static readonly string[] ArRoles = PmsRoles.AccountsReceivable;
    private static readonly string[] FbRoles = PmsRoles.FoodBeverageService;
    private static readonly string[] KitchenRoles = PmsRoles.FoodBeverageKitchen;
    private static readonly string[] BanquetRoles = PmsRoles.Banquet;
    private static readonly string[] SalesRoles = PmsRoles.Sales;
    private static readonly string[] RevenueRoles = PmsRoles.Revenue;
    private static readonly string[] BookingRoles = PmsRoles.BookingEngineManagement;
    private static readonly string[] GuestPortalRoles = PmsRoles.GuestPortalManagement;
    private static readonly string[] InventoryRoles = PmsRoles.InventoryPurchasing;
    private static readonly string[] LaborRoles = PmsRoles.LaborCosting;
    private static readonly string[] AiRoles = PmsRoles.ManagementAI;
    private static readonly string[] ExecutiveRoles = PmsRoles.ExecutiveReporting;
    private static readonly string[] SystemRoles = PmsRoles.SystemManagement;
    private static readonly string[] GroupRoles = PmsRoles.GroupManagement;

    public static IReadOnlyList<ReportCatalogEntry> DefaultCatalog { get; } =
    [
        Item("occupancy-report", "Occupancy Report", ReportCategory.FrontOffice, "Room inventory, occupied rooms, and occupancy percentage.", "/Reports/Occupancy", OperationalRoles),
        Item("arrivals-report", "Arrivals Report", ReportCategory.FrontOffice, "Expected arrivals for the business date.", "/Reports/Arrivals", OperationalRoles),
        Item("departures-report", "Departures Report", ReportCategory.FrontOffice, "Expected departures for the business date.", "/Reports/Departures", OperationalRoles),
        Item("in-house-guest-report", "In-House Guest Report", ReportCategory.FrontOffice, "Checked-in guests and current balances.", "/Reports/InHouse", OperationalRoles),
        Item("no-show-report", "No-Show Report", ReportCategory.FrontOffice, "No-show reservation monitoring.", null, OperationalRoles),
        Item("reservation-forecast", "Reservation Forecast", ReportCategory.FrontOffice, "Forward reservation demand and stay activity.", null, OperationalRoles),
        Item("room-status-report", "Room Status Report", ReportCategory.FrontOffice, "Current room readiness and status distribution.", "/Housekeeping/Index", OperationalRoles),
        Item("group-booking-list", "Group Booking List", ReportCategory.FrontOffice, "Group bookings, statuses, dates, and collection readiness.", "/Groups/Reports/Index", GroupRoles),
        Item("group-room-block-report", "Group Room Block Report", ReportCategory.FrontOffice, "Room blocks by group, date, room type, and pickup.", "/Groups/Reports/Index", GroupRoles),
        Item("group-pickup-report", "Group Pickup Report", ReportCategory.FrontOffice, "Picked-up rooms and remaining group block inventory.", "/Groups/Reports/Index", GroupRoles),
        Item("group-pace-report", "Group Pace Report", ReportCategory.FrontOffice, "Group pickup pace and remaining block demand.", "/Groups/Reports/Index", GroupRoles),

        Item("housekeeping-room-status", "Room Status Report", ReportCategory.Housekeeping, "Housekeeping room status and readiness view.", "/Housekeeping/Index", HousekeepingRoles),
        Item("housekeeping-task-report", "Housekeeping Task Report", ReportCategory.Housekeeping, "Open and completed housekeeping tasks.", "/Housekeeping/Tasks/Index", HousekeepingRoles),
        Item("dirty-rooms-report", "Dirty Rooms Report", ReportCategory.Housekeeping, "Rooms requiring cleaning attention.", null, HousekeepingRoles),
        Item("out-of-order-rooms-report", "Out of Order Rooms Report", ReportCategory.Housekeeping, "Blocked and out-of-order room inventory.", null, HousekeepingRoles),
        Item("lost-and-found-report", "Lost and Found Report", ReportCategory.Housekeeping, "Lost and found tracking report awaiting property configuration.", null, HousekeepingRoles),

        Item("daily-revenue-report", "Daily Revenue Report", ReportCategory.Finance, "Charges, payments, and outstanding balances.", "/Reports/DailyRevenue", FinanceRoles),
        Item("payment-summary-report", "Payment Summary Report", ReportCategory.Finance, "Payment collection summary by method.", null, FinanceRoles),
        Item("cashier-shift-report", "Cashier Shift Report", ReportCategory.Finance, "Cashier shift accountability and cash control.", "/Finance/CashierShifts/Index", FinanceRoles),
        Item("cashier-transaction-report", "Cashier Transaction Report", ReportCategory.Finance, "Cashier transaction detail.", null, FinanceRoles),
        Item("refund-report", "Refund Report", ReportCategory.Finance, "Refund request and approval report.", "/Finance/Refunds/Index", FinanceRoles),
        Item("void-request-report", "Void Request Report", ReportCategory.Finance, "Void request and approval report.", "/Finance/VoidRequests/Index", FinanceRoles),
        Item("discount-approval-report", "Discount Approval Report", ReportCategory.Finance, "Discount approval tracking.", "/Finance/DiscountApprovals/Index", FinanceRoles),
        Item("finance-document-register", "Finance Document Register", ReportCategory.Finance, "Finance document register and print source.", "/Finance/Documents/Index", FinanceRoles),
        Item("group-master-folio-report", "Group Master Folio Report", ReportCategory.Finance, "Group master folio charges, payments, and balances.", "/Groups/Reports/Index", GroupRoles),
        Item("group-collection-report", "Group Collection Report", ReportCategory.Finance, "Group deposits, allocations, payments, and outstanding balances.", "/Groups/Reports/Index", GroupRoles),
        Item("pseudo-room-folio-report", "Pseudo Room Folio Report", ReportCategory.Finance, "Paymaster and pseudo room folio monitoring.", "/Groups/Reports/Index", GroupRoles),
        Item("charge-routing-report", "Charge Routing Report", ReportCategory.Finance, "Active routing rules for group and pseudo room billing.", "/Groups/Reports/Index", GroupRoles),
        Item("group-ar-transfer-report", "Group AR Transfer Report", ReportCategory.Finance, "Group folios prepared for AR transfer review.", "/Groups/Reports/Index", GroupRoles),

        Item("general-ledger", "General Ledger", ReportCategory.Accounting, "Posted journal entry lines by GL account.", "/Accounting/Reports/GeneralLedger", FinanceRoles, true),
        Item("trial-balance", "Trial Balance", ReportCategory.Accounting, "Debit and credit balances from posted journal entries.", "/Accounting/Reports/TrialBalance", FinanceRoles, true),
        Item("balance-sheet", "Balance Sheet", ReportCategory.Accounting, "Assets, liabilities, and equity as of date.", "/Accounting/Reports/BalanceSheet", FinanceRoles, true),
        Item("profit-loss", "Profit and Loss", ReportCategory.Accounting, "Standard accounting P&L from posted GL activity.", "/Accounting/Reports/ProfitAndLoss", FinanceRoles, true),
        Item("statement-of-cash-flows", "Statement of Cash Flows", ReportCategory.Accounting, "Operating, investing, and financing cash movement from posted journal entries.", "/Accounting/Reports/StatementOfCashFlows", FinanceRoles, true),
        Item("cash-movement-report", "Cash Movement Report", ReportCategory.Accounting, "Cash inflow and outflow detail by source, account, and cash flow category.", "/Accounting/Reports/CashMovement", FinanceRoles, true),
        Item("unmapped-cash-flow-items", "Unmapped Cash Flow Items", ReportCategory.Accounting, "Cash movements requiring mapping review.", "/Accounting/Reports/UnmappedCashFlowItems", FinanceRoles, true),
        Item("cash-flow-snapshots", "Cash Flow Snapshots", ReportCategory.Accounting, "Saved Statement of Cash Flows runs for owner and finance review.", "/Accounting/Reports/CashFlowSnapshots", FinanceRoles),
        Item("sales-journal", "Sales Journal", ReportCategory.Accounting, "Internal tax-support sales journal for finance review.", "/Accounting/Reports/SalesJournal", FinanceRoles),
        Item("purchase-journal", "Purchase Journal", ReportCategory.Accounting, "Internal tax-support purchase journal for finance review.", "/Accounting/Reports/PurchaseJournal", FinanceRoles),
        Item("cash-receipts-journal", "Cash Receipts Journal", ReportCategory.Accounting, "Internal tax-support cash receipts journal from payments and collections.", "/Accounting/Reports/CashReceiptsJournal", FinanceRoles),
        Item("cash-disbursements-journal", "Cash Disbursements Journal", ReportCategory.Accounting, "Controlled disbursement journal view for finance review.", "/Accounting/Reports/CashDisbursementsJournal", FinanceRoles),

        Item("usali-operating-statement", "USALI Operating Statement", ReportCategory.USALI, "USALI-style operating statement for management review.", "/Accounting/Reports/USALI", FinanceRoles, true),
        Item("rooms-department-pl", "Rooms Department P&L", ReportCategory.USALI, "Rooms department P&L summary.", "/Accounting/Reports/RoomsPL", FinanceRoles),
        Item("fb-department-pl", "F&B Department P&L", ReportCategory.USALI, "Food and beverage department P&L summary.", "/Accounting/Reports/FoodBeveragePL", FinanceRoles),
        Item("banquet-department-pl", "Banquet Department P&L", ReportCategory.USALI, "Banquet department P&L summary.", "/Accounting/Reports/BanquetPL", FinanceRoles),
        Item("other-operated-departments-pl", "Other Operated Departments P&L", ReportCategory.USALI, "Other operated departments P&L summary.", "/Accounting/Reports/OtherOperatedDepartmentsPL", FinanceRoles),

        Item("sales-invoice-register", "Sales Invoice Register", ReportCategory.PhilippineFinance, "Internal sales invoice register for finance review; CPA validation required.", "/Accounting/Reports/SalesInvoiceRegister", FinanceRoles),
        Item("payment-receipt-register", "Payment Receipt Register", ReportCategory.PhilippineFinance, "Internal payment receipt register for finance review; CPA validation required.", "/Accounting/Reports/PaymentReceiptRegister", FinanceRoles),
        Item("vat-output-summary", "VAT Output Summary", ReportCategory.PhilippineFinance, "Tax-support output VAT summary; not a BIR filing output.", "/Accounting/Reports/VATOutput", FinanceRoles),
        Item("vat-input-summary", "VAT Input Summary", ReportCategory.PhilippineFinance, "Tax-support input VAT summary; not a BIR filing output.", "/Accounting/Reports/VATInput", FinanceRoles),
        Item("vat-payable-summary", "VAT Payable Summary", ReportCategory.PhilippineFinance, "Tax-support VAT payable summary for accountant review.", "/Accounting/Reports/VATPayable", FinanceRoles),
        Item("expanded-withholding-tax-summary", "Expanded Withholding Tax Summary", ReportCategory.PhilippineFinance, "Withholding tax-support summary; official formatting requires accountant validation.", "/Accounting/Reports/WithholdingTaxSummary", FinanceRoles),
        Item("creditable-withholding-tax-summary", "Creditable Withholding Tax Summary", ReportCategory.PhilippineFinance, "Creditable withholding tax-support summary; official formatting requires accountant validation.", "/Accounting/Reports/WithholdingTaxSummary", FinanceRoles),
        Item("books-of-accounts-export", "Books of Accounts Export Review", ReportCategory.PhilippineFinance, "Controlled books export workspace for accountant validation; no real BIR submission.", "/Accounting/Reports/BooksExport", FinanceRoles),

        Item("ar-aging", "AR Aging Report", ReportCategory.AccountsReceivable, "Invoice-level accounts receivable aging by due date and collection bucket.", "/AccountsReceivable/Aging/Index", ArRoles, true),
        Item("ar-collection-daily", "Daily AR Collection Report", ReportCategory.AccountsReceivable, "Daily opening AR, billings, collections, memos, and ending AR.", "/AccountsReceivable/Collections/Index?period=daily", ArRoles, true),
        Item("ar-collection-weekly", "Weekly AR Collection Report", ReportCategory.AccountsReceivable, "Weekly AR collection movement, overdue risk, and account follow-up view.", "/AccountsReceivable/Collections/Index?period=weekly", ArRoles, true),
        Item("ar-collection-monthly", "Monthly AR Collection Report", ReportCategory.AccountsReceivable, "Monthly AR movement, collection rate, DSO, and stale account review.", "/AccountsReceivable/Collections/Index?period=monthly", ArRoles, true),
        Item("ar-collection-custom", "Custom AR Collection Report", ReportCategory.AccountsReceivable, "Custom date range AR collection report for finance review.", "/AccountsReceivable/Collections/Index?period=custom", ArRoles, true),
        Item("statement-of-account", "Statement of Account", ReportCategory.AccountsReceivable, "Printable statement of account.", "/Documents/Index", ArRoles),
        Item("ar-invoice-register", "AR Invoice Register", ReportCategory.AccountsReceivable, "AR invoice register.", "/AccountsReceivable/ARInvoices/Index", ArRoles),
        Item("ar-payment-register", "AR Payment Register", ReportCategory.AccountsReceivable, "AR payment register.", "/AccountsReceivable/ARPayments/Index", ArRoles),
        Item("credit-memo-report", "Credit Memo Report", ReportCategory.AccountsReceivable, "Credit memo report.", "/AccountsReceivable/CreditMemos/Index", ArRoles),
        Item("debit-memo-report", "Debit Memo Report", ReportCategory.AccountsReceivable, "Debit memo report.", "/AccountsReceivable/DebitMemos/Index", ArRoles),

        Item("ap-aging", "AP Aging", ReportCategory.AccountsPayable, "Supplier payable aging buckets.", "/Accounting/Reports/APAging", FinanceRoles, true),
        Item("ap-invoice-register", "AP Invoice Register", ReportCategory.AccountsPayable, "AP invoice register.", "/Accounting/Reports/APInvoiceRegister", FinanceRoles),
        Item("payment-voucher-register", "Payment Voucher Register", ReportCategory.AccountsPayable, "Payment voucher register.", "/Accounting/Reports/PaymentVoucherRegister", FinanceRoles, true),
        Item("supplier-ledger", "Supplier Ledger", ReportCategory.AccountsPayable, "Supplier invoice and payment activity.", "/Accounting/Reports/SupplierLedger", FinanceRoles, true),
        Item("disbursement-summary", "Disbursement Summary", ReportCategory.AccountsPayable, "Released supplier payments.", "/Accounting/Reports/DisbursementSummary", FinanceRoles),

        Item("bank-transactions-report", "Bank Transactions Report", ReportCategory.Banking, "Bank account debit and credit activity.", "/Accounting/Reports/BankTransactionsReport", FinanceRoles),
        Item("bank-reconciliation-report", "Bank Reconciliation Report", ReportCategory.Banking, "Prepared and approved bank reconciliations.", "/Accounting/Reports/BankReconciliationReport", FinanceRoles),
        Item("cash-position-report", "Cash Position Report", ReportCategory.Banking, "Cash and bank position by account.", "/Accounting/Reports/CashPosition", FinanceRoles),

        Item("payroll-cost-summary", "Payroll Cost Summary", ReportCategory.PayrollLabor, "Payroll cost summary by period and department.", "/Labor/Reports/Index", LaborRoles),
        Item("payroll-cost-by-department", "Payroll Cost by Department", ReportCategory.PayrollLabor, "Payroll cost by operating department.", "/Labor/Reports/Index", LaborRoles),
        Item("payroll-cost-by-usali", "Payroll Cost by USALI Department", ReportCategory.PayrollLabor, "Payroll cost by USALI department.", "/Labor/Reports/Index", LaborRoles),
        Item("labor-budget-vs-actual", "Labor Budget vs Actual", ReportCategory.PayrollLabor, "Labor budget variance report.", "/Labor/Reports/Index", LaborRoles),
        Item("service-charge-distribution", "Service Charge Distribution Report", ReportCategory.PayrollLabor, "Service charge pool and distribution detail.", "/Labor/Reports/Index", LaborRoles),
        Item("labor-productivity-report", "Labor Productivity Report", ReportCategory.PayrollLabor, "Labor cost and productivity summary.", "/Labor/Reports/Index", LaborRoles),
        Item("payroll-journal-register", "Payroll Journal Register", ReportCategory.PayrollLabor, "Payroll journal register.", "/Labor/Reports/Index", LaborRoles),

        Item("fb-sales-report", "F&B Sales Report", ReportCategory.FoodBeverage, "F&B sales by outlet and status.", "/FoodBeverage/Index", FbRoles),
        Item("sales-by-outlet", "Sales by Outlet", ReportCategory.FoodBeverage, "Outlet-level sales report.", null, FbRoles),
        Item("sales-by-item", "Sales by Item", ReportCategory.FoodBeverage, "Menu item sales report.", null, FbRoles),
        Item("sales-by-category", "Sales by Category", ReportCategory.FoodBeverage, "Menu category sales report.", null, FbRoles),
        Item("fb-void-report", "Void Report", ReportCategory.FoodBeverage, "F&B void report.", null, FbRoles),
        Item("fb-discount-report", "Discount Report", ReportCategory.FoodBeverage, "F&B discount report.", null, FbRoles),
        Item("room-charge-report", "Room Charge Report", ReportCategory.FoodBeverage, "Room charge order report.", null, FbRoles),

        Item("kitchen-order-report", "Kitchen Order Report", ReportCategory.Kitchen, "Kitchen order ticket and status report.", "/FoodBeverageKitchen/Index", KitchenRoles),
        Item("delayed-items-report", "Delayed Items Report", ReportCategory.Kitchen, "Delayed kitchen items report.", null, KitchenRoles),
        Item("station-performance-report", "Station Performance Report", ReportCategory.Kitchen, "Kitchen station performance report awaiting outlet configuration.", null, KitchenRoles),

        Item("banquet-event-report", "Banquet Event Report", ReportCategory.Banquet, "Banquet events and status report.", "/Banquet/Index", BanquetRoles),
        Item("banquet-revenue-report", "Banquet Revenue Report", ReportCategory.Banquet, "Banquet charges and revenue report.", null, BanquetRoles),
        Item("beo-list", "Banquet Event Order List", ReportCategory.Banquet, "BEO readiness list.", "/Documents/Index", BanquetRoles),
        Item("function-room-utilization", "Function Room Utilization", ReportCategory.Banquet, "Function room utilization report awaiting event calendar configuration.", null, BanquetRoles),
        Item("event-profitability-report", "Event Profitability Report", ReportCategory.Banquet, "Banquet event profitability report awaiting charge and cost mapping.", null, BanquetRoles),

        Item("sales-account-report", "Sales Account Report", ReportCategory.Sales, "Sales account production and contacts.", "/Sales/Index", SalesRoles),
        Item("sales-pipeline-report", "Sales Pipeline Report", ReportCategory.Sales, "Sales lead pipeline report.", null, SalesRoles),
        Item("sales-activity-report", "Sales Activity Report", ReportCategory.Sales, "Sales activity report.", null, SalesRoles),
        Item("account-production-report", "Account Production Report", ReportCategory.Sales, "Account production report awaiting sales production mapping.", null, SalesRoles),

        Item("revenue-dashboard", "Revenue Dashboard", ReportCategory.Revenue, "Revenue dashboard and controls.", "/Revenue/Index", RevenueRoles),
        Item("revenue-calendar", "Revenue Calendar", ReportCategory.Revenue, "Rate and inventory calendar.", "/Revenue/Calendar/Index", RevenueRoles),
        Item("rate-plan-report", "Rate Plan Report", ReportCategory.Revenue, "Rate plan setup and production report awaiting revenue mapping.", null, RevenueRoles),
        Item("promotion-usage-report", "Promotion Usage Report", ReportCategory.Revenue, "Promo code usage report awaiting booking conversion mapping.", null, RevenueRoles),
        Item("booking-pace-report", "Booking Pace Report", ReportCategory.Revenue, "Booking pace report awaiting pickup history configuration.", null, RevenueRoles),

        Item("booking-request-report", "Booking Request Report", ReportCategory.BookingEngine, "Booking engine request report.", "/BookingManagement/Requests/Index", BookingRoles),
        Item("booking-conversion-report", "Booking Conversion Report", ReportCategory.BookingEngine, "Booking conversion report awaiting channel analytics configuration.", null, BookingRoles),
        Item("promo-code-usage-report", "Promo Code Usage Report", ReportCategory.BookingEngine, "Promo usage report awaiting booking engine analytics configuration.", null, BookingRoles),

        Item("pre-check-in-report", "Pre-Check-In Report", ReportCategory.GuestPortal, "Guest portal pre-check-in report.", "/GuestPortalManagement/PreCheckIns/Index", GuestPortalRoles),
        Item("guest-service-request-report", "Guest Service Request Report", ReportCategory.GuestPortal, "Guest service request report.", "/GuestPortalManagement/ServiceRequests/Index", GuestPortalRoles),
        Item("guest-feedback-report", "Guest Feedback Report", ReportCategory.GuestPortal, "Guest feedback report.", "/GuestPortalManagement/Feedback/Index", GuestPortalRoles),
        Item("express-checkout-request-report", "Express Checkout Request Report", ReportCategory.GuestPortal, "Express checkout report.", "/GuestPortalManagement/ExpressCheckout/Index", GuestPortalRoles),

        Item("stock-on-hand", "Stock on Hand Report", ReportCategory.Inventory, "Inventory stock on hand and value.", "/Inventory/Index", InventoryRoles, true),
        Item("low-stock", "Low Stock Report", ReportCategory.Inventory, "Items at or below reorder level.", "/Inventory/Index", InventoryRoles, true),
        Item("expiring-items-report", "Expiring Items Report", ReportCategory.Inventory, "Perishable items expiring soon.", null, InventoryRoles),
        Item("stock-movement", "Stock Movement Report", ReportCategory.Inventory, "Inventory movement history.", "/Inventory/StockMovements/Index", InventoryRoles, true),
        Item("stock-adjustment-report", "Stock Adjustment Report", ReportCategory.Inventory, "Stock adjustments and variances.", "/Inventory/StockAdjustments/Index", InventoryRoles),

        Item("purchase-request-summary", "Purchase Request Summary", ReportCategory.Purchasing, "Purchase request summary.", "/Purchasing/PurchaseRequests/Index", InventoryRoles),
        Item("purchase-order-summary", "Purchase Order Summary", ReportCategory.Purchasing, "Purchase order summary.", "/Purchasing/PurchaseOrders/Index", InventoryRoles),
        Item("receiving-summary", "Receiving Summary", ReportCategory.Purchasing, "Receiving record summary.", "/Purchasing/Receiving/Index", InventoryRoles),
        Item("supplier-summary", "Supplier Summary", ReportCategory.Purchasing, "Supplier master list summary.", "/Purchasing/Suppliers/Index", InventoryRoles),

        Item("daily-management-summary", "Daily Management Summary", ReportCategory.ManagementAI, "Rule-based daily management summary.", "/ManagementAI/Index", AiRoles),
        Item("management-insights", "Management Insights", ReportCategory.ManagementAI, "Management insight list.", "/ManagementAI/Insights/Index", AiRoles),
        Item("recommended-actions", "Recommended Actions", ReportCategory.ManagementAI, "Recommended actions from configured rules.", "/ManagementAI/Index", AiRoles),
        Item("ai-action-log", "AI Action Log", ReportCategory.ManagementAI, "Management AI action log.", "/ManagementAI/ActionLog/Index", AiRoles),

        Item("executive-dashboard", "Executive Dashboard", ReportCategory.Executive, "Owner/GM executive command center.", "/Executive/Index", ExecutiveRoles),
        Item("kpi-scorecard", "KPI Scorecard", ReportCategory.Executive, "Executive KPI scorecard.", "/Executive/KPIScorecard", ExecutiveRoles, true),
        Item("daily-flash-report", "Daily Flash Report", ReportCategory.Executive, "Daily flash report for owners and managers.", "/Executive/DailyFlash", ExecutiveRoles),
        Item("weekly-executive-summary", "Weekly Executive Summary", ReportCategory.Executive, "Weekly executive trends and risks.", "/Executive/WeeklySummary", ExecutiveRoles),
        Item("monthly-owner-report", "Monthly Owner Report", ReportCategory.Executive, "Owner-ready monthly report.", "/Executive/MonthlyOwnerReport", ExecutiveRoles),
        Item("department-performance", "Department Performance", ReportCategory.Executive, "Department revenue, cost, labor, and profit.", "/Executive/DepartmentPerformance", ExecutiveRoles, true),
        Item("revenue-intelligence", "Revenue Intelligence", ReportCategory.Executive, "Forward demand and revenue opportunity intelligence.", "/Executive/RevenueIntelligence", ExecutiveRoles),
        Item("guest-experience-intelligence", "Guest Experience Intelligence", ReportCategory.Executive, "Guest service and feedback intelligence.", "/Executive/GuestExperience", ExecutiveRoles),
        Item("finance-control-intelligence", "Finance Control Intelligence", ReportCategory.Executive, "Finance control and close-readiness intelligence.", "/Executive/FinanceControl", ExecutiveRoles),
        Item("cost-control-intelligence", "Cost Control Intelligence", ReportCategory.Executive, "Labor, inventory, AP, and cost control signals.", "/Executive/CostControl", ExecutiveRoles),
        Item("owner-report-package", "Owner Report Package", ReportCategory.Executive, "Printable owner report packages.", "/Executive/OwnerPackages/Index", ExecutiveRoles),
        Item("executive-alerts", "Executive Alerts", ReportCategory.Executive, "Executive risk and opportunity alerts.", "/Executive/Alerts/Index", ExecutiveRoles, true),

        Item("audit-log-report", "Audit Log Report", ReportCategory.AuditSystem, "System audit trail report.", "/System/AuditLogs/Index", SystemRoles),
        Item("system-error-log-report", "System Error Log Report", ReportCategory.AuditSystem, "System error log report.", "/System/ErrorLogs/Index", [PmsRoles.SystemAdmin]),
        Item("system-health-check-report", "System Health Check Report", ReportCategory.AuditSystem, "System health check report.", "/System/HealthCheck/Index", SystemRoles),
        Item("qa-checklist-report", "QA Checklist Report", ReportCategory.AuditSystem, "QA checklist report.", "/System/QAChecklist/Index", SystemRoles),
        Item("module-qa-stabilization", "Module QA Stabilization", ReportCategory.AuditSystem, "End-to-end module workflow QA stabilization tracker.", "/System/ModuleQA/Index", SystemRoles),
        Item("data-validation-issues-report", "Data Validation Issues Report", ReportCategory.AuditSystem, "Data validation issue report.", "/System/HealthCheck/Index", SystemRoles)
    ];

    private static ReportCatalogEntry Item(
        string reportKey,
        string reportName,
        ReportCategory category,
        string description,
        string? routePath,
        string[] requiredRoles,
        bool supportsCsv = false)
    {
        return new ReportCatalogEntry(
            reportKey,
            reportName,
            category,
            description,
            routePath,
            requiredRoles,
            SupportsPrint: routePath is not null,
            SupportsCsvExport: supportsCsv,
            SupportsHtmlExport: routePath is not null,
            SupportsPdfViaBrowserPrint: routePath is not null);
    }
}

public record ReportCatalogEntry(
    string ReportKey,
    string ReportName,
    ReportCategory ReportCategory,
    string Description,
    string? RoutePath,
    string[] RequiredRoles,
    bool SupportsPrint,
    bool SupportsCsvExport,
    bool SupportsHtmlExport,
    bool SupportsPdfViaBrowserPrint)
{
    public string RequiredRolesText => string.Join(", ", RequiredRoles);

    public bool IsAvailable => !string.IsNullOrWhiteSpace(RoutePath);
}
