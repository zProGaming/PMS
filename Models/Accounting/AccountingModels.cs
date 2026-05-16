using Vantage.PMS.Models.Core;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Models.Accounting;

public enum GLAccountType
{
    Asset = 0,
    Liability = 1,
    Equity = 2,
    Revenue = 3,
    CostOfSales = 4,
    Expense = 5,
    OtherIncome = 6,
    OtherExpense = 7
}

public enum NormalBalance
{
    Debit = 0,
    Credit = 1
}

public enum USALIDepartmentType
{
    OperatedDepartment = 0,
    UndistributedOperatingExpense = 1,
    FixedCharge = 2,
    NonOperating = 3,
    Management = 4,
    Other = 5
}

public enum USALIReportSection
{
    Rooms = 0,
    FoodAndBeverage = 1,
    Banquet = 2,
    OtherOperatedDepartments = 3,
    AdministrativeAndGeneral = 4,
    SalesAndMarketing = 5,
    PropertyOperationsAndMaintenance = 6,
    Utilities = 7,
    InformationAndTelecommunications = 8,
    ManagementFees = 9,
    NonOperatingIncomeExpense = 10,
    FixedCharges = 11,
    GrossOperatingProfit = 12,
    EBITDA = 13,
    NetIncome = 14
}

public enum AccountingPeriodStatus
{
    Open = 0,
    Closed = 1,
    Locked = 2
}

public enum JournalEntryStatus
{
    Draft = 0,
    Posted = 1,
    Reversed = 2,
    Cancelled = 3
}

public enum SourceModule
{
    FrontOffice = 0,
    Finance = 1,
    FoodBeverage = 2,
    Banquet = 3,
    Inventory = 4,
    Purchasing = 5,
    AccountsReceivable = 6,
    NightAudit = 7,
    Manual = 8,
    System = 9
}

public enum SourceTransactionType
{
    RoomCharge = 0,
    FolioCharge = 1,
    FolioPayment = 2,
    FolioDiscount = 3,
    FolioRefund = 4,
    POSCharge = 5,
    POSChargeToRoom = 6,
    POSPayment = 7,
    BanquetCharge = 8,
    BanquetPayment = 9,
    ARInvoice = 10,
    ARPayment = 11,
    ARCreditMemo = 12,
    ARDebitMemo = 13,
    PurchaseReceiving = 14,
    StockIssue = 15,
    StockAdjustmentIncrease = 16,
    StockAdjustmentDecrease = 17,
    CashDrop = 18,
    ManualJournal = 19,
    NightAuditPosting = 20,
    APInvoice = 21,
    PaymentVoucher = 22,
    Disbursement = 23,
    BankAdjustment = 24,
    Accrual = 25,
    PayrollCost = 26,
    ServiceChargeDistribution = 27
}

public enum PostingBatchStatus
{
    Draft = 0,
    Processing = 1,
    Posted = 2,
    PostedWithErrors = 3,
    Cancelled = 4
}

public enum PostingBatchItemStatus
{
    Pending = 0,
    Posted = 1,
    Error = 2,
    Skipped = 3
}

public enum TaxType
{
    VATOutput = 0,
    VATInput = 1,
    WithholdingTax = 2,
    PercentageTax = 3,
    Other = 4
}

public enum PhilippineReportType
{
    SalesInvoiceRegister = 0,
    PaymentReceiptRegister = 1,
    VATOutputSummary = 2,
    VATInputSummary = 3,
    VATPayableSummary = 4,
    ExpandedWithholdingTaxSummary = 5,
    CreditableWithholdingTaxSummary = 6,
    SalesJournal = 7,
    PurchaseJournal = 8,
    CashReceiptsJournal = 9,
    CashDisbursementsJournal = 10,
    GeneralLedger = 11,
    TrialBalance = 12,
    ProfitAndLoss = 13,
    BalanceSheet = 14,
    AccountsReceivableSubsidiaryLedger = 15,
    AccountsPayableSubsidiaryLedger = 16
}

public class GLAccount
{
    public int Id { get; set; }

    public string AccountCode { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public GLAccountType AccountType { get; set; }

    public NormalBalance NormalBalance { get; set; }

    public int? ParentGLAccountId { get; set; }

    public GLAccount? ParentGLAccount { get; set; }

    public ICollection<GLAccount> ChildAccounts { get; set; } = new List<GLAccount>();

    public int? UsaliDepartmentId { get; set; }

    public USALIDepartment? UsaliDepartment { get; set; }

    public int? UsaliReportLineId { get; set; }

    public USALIReportLine? UsaliReportLine { get; set; }

    public string? PhilippineReportCategory { get; set; }

    public bool IsControlAccount { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;
}

public class USALIDepartment
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public USALIDepartmentType DepartmentType { get; set; } = USALIDepartmentType.OperatedDepartment;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class USALIReportLine
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public USALIReportSection ReportSection { get; set; }

    public int? ParentUSALIReportLineId { get; set; }

    public USALIReportLine? ParentUSALIReportLine { get; set; }

    public ICollection<USALIReportLine> ChildLines { get; set; } = new List<USALIReportLine>();

    public int SortOrder { get; set; }

    public bool IsSubtotal { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AccountingPeriod
{
    public int Id { get; set; }

    public string PeriodName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Open;

    public string? ClosedBy { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? Notes { get; set; }
}

public class JournalEntry
{
    public int Id { get; set; }

    public string JournalNumber { get; set; } = string.Empty;

    public DateTime JournalDate { get; set; } = DateTime.Today;

    public int? AccountingPeriodId { get; set; }

    public AccountingPeriod? AccountingPeriod { get; set; }

    public SourceModule SourceModule { get; set; } = SourceModule.Manual;

    public SourceTransactionType SourceTransactionType { get; set; } = SourceTransactionType.ManualJournal;

    public int? SourceReferenceId { get; set; }

    public string? SourceReferenceNumber { get; set; }

    public string Description { get; set; } = string.Empty;

    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;

    public string? PostedBy { get; set; }

    public DateTime? PostedAt { get; set; }

    public string? ReversedBy { get; set; }

    public DateTime? ReversedAt { get; set; }

    public string? ReversalReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();

    public decimal TotalDebits => Lines.Sum(line => line.DebitAmount);

    public decimal TotalCredits => Lines.Sum(line => line.CreditAmount);

    public bool IsBalanced => TotalDebits == TotalCredits;
}

public class JournalEntryLine
{
    public int Id { get; set; }

    public int JournalEntryId { get; set; }

    public JournalEntry? JournalEntry { get; set; }

    public int GLAccountId { get; set; }

    public GLAccount? GLAccount { get; set; }

    public int? USALIDepartmentId { get; set; }

    public USALIDepartment? USALIDepartment { get; set; }

    public decimal DebitAmount { get; set; }

    public decimal CreditAmount { get; set; }

    public string? Description { get; set; }

    public string? LineReferenceType { get; set; }

    public int? LineReferenceId { get; set; }
}

public class PostingRule
{
    public int Id { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public SourceModule SourceModule { get; set; }

    public SourceTransactionType TransactionType { get; set; }

    public int? ChargeCodeId { get; set; }

    public ChargeCode? ChargeCode { get; set; }

    public string? PaymentMethod { get; set; }

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int? USALIDepartmentId { get; set; }

    public USALIDepartment? USALIDepartment { get; set; }

    public int DebitGLAccountId { get; set; }

    public GLAccount? DebitGLAccount { get; set; }

    public int CreditGLAccountId { get; set; }

    public GLAccount? CreditGLAccount { get; set; }

    public int? TaxGLAccountId { get; set; }

    public GLAccount? TaxGLAccount { get; set; }

    public int? ServiceChargeGLAccountId { get; set; }

    public GLAccount? ServiceChargeGLAccount { get; set; }

    public int? DiscountGLAccountId { get; set; }

    public GLAccount? DiscountGLAccount { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}

public class PostingBatch
{
    public int Id { get; set; }

    public string BatchNumber { get; set; } = string.Empty;

    public DateTime BatchDate { get; set; } = DateTime.Today;

    public SourceModule SourceModule { get; set; }

    public PostingBatchStatus Status { get; set; } = PostingBatchStatus.Draft;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? PostedBy { get; set; }

    public DateTime? PostedAt { get; set; }

    public string? Notes { get; set; }

    public ICollection<PostingBatchItem> Items { get; set; } = new List<PostingBatchItem>();
}

public class PostingBatchItem
{
    public int Id { get; set; }

    public int PostingBatchId { get; set; }

    public PostingBatch? PostingBatch { get; set; }

    public SourceTransactionType SourceTransactionType { get; set; }

    public int SourceReferenceId { get; set; }

    public string? SourceReferenceNumber { get; set; }

    public int? JournalEntryId { get; set; }

    public JournalEntry? JournalEntry { get; set; }

    public PostingBatchItemStatus Status { get; set; } = PostingBatchItemStatus.Pending;

    public string? ErrorMessage { get; set; }
}

public class TaxCode
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public TaxType TaxType { get; set; }

    public decimal Rate { get; set; }

    public int? GLAccountId { get; set; }

    public GLAccount? GLAccount { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}

public class ServiceChargeSetting
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public int? LiabilityGLAccountId { get; set; }

    public GLAccount? LiabilityGLAccount { get; set; }

    public int? RevenueGLAccountId { get; set; }

    public GLAccount? RevenueGLAccount { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}

public class PhilippineTaxReportLine
{
    public int Id { get; set; }

    public PhilippineReportType ReportType { get; set; }

    public string LineCode { get; set; } = string.Empty;

    public string LineName { get; set; } = string.Empty;

    public int? GLAccountId { get; set; }

    public GLAccount? GLAccount { get; set; }

    public int? TaxCodeId { get; set; }

    public TaxCode? TaxCode { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AccountingReportSnapshot
{
    public int Id { get; set; }

    public string ReportName { get; set; } = string.Empty;

    public PhilippineReportType ReportType { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    public string GeneratedBy { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public ICollection<AccountingReportSnapshotLine> Lines { get; set; } = new List<AccountingReportSnapshotLine>();
}

public class AccountingReportSnapshotLine
{
    public int Id { get; set; }

    public int AccountingReportSnapshotId { get; set; }

    public AccountingReportSnapshot? AccountingReportSnapshot { get; set; }

    public string LineCode { get; set; } = string.Empty;

    public string LineName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int SortOrder { get; set; }
}

public class AccountingExportLog
{
    public int Id { get; set; }

    public DateTime ExportDate { get; set; } = DateTime.Now;

    public PhilippineReportType ReportType { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ExportedBy { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
