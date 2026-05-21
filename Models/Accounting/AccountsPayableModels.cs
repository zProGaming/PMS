using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Models.Accounting;

public enum APInvoiceStatus
{
    Draft = 0,
    ForApproval = 1,
    Approved = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5,
    Voided = 6
}

public enum PaymentVoucherStatus
{
    Draft = 0,
    ForApproval = 1,
    Approved = 2,
    Released = 3,
    Cancelled = 4,
    Voided = 5
}

public enum DisbursementStatus
{
    Draft = 0,
    Released = 1,
    Cleared = 2,
    Cancelled = 3,
    Voided = 4
}

public enum BankReconciliationStatus
{
    Draft = 0,
    Balanced = 1,
    ForReview = 2,
    Approved = 3,
    Cancelled = 4
}

public enum BankReconciliationItemType
{
    Deposit = 0,
    Withdrawal = 1,
    BankCharge = 2,
    Interest = 3,
    OutstandingCheck = 4,
    DepositInTransit = 5,
    Adjustment = 6
}

public enum AccrualEntryStatus
{
    Draft = 0,
    ForApproval = 1,
    Approved = 2,
    Posted = 3,
    Reversed = 4,
    Cancelled = 5
}

public enum MonthEndChecklistStatus
{
    Pending = 0,
    Completed = 1,
    NotApplicable = 2,
    IssueFound = 3
}

public class APInvoice
{
    public int Id { get; set; }

    public int SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public int? PurchaseOrderId { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public int? ReceivingRecordId { get; set; }

    public ReceivingRecord? ReceivingRecord { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string? SupplierInvoiceNumber { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal WithholdingTaxAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Balance { get; set; }

    public APInvoiceStatus Status { get; set; } = APInvoiceStatus.Draft;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int? JournalEntryId { get; set; }

    public JournalEntry? JournalEntry { get; set; }

    public ICollection<APInvoiceLine> Lines { get; set; } = new List<APInvoiceLine>();

    public ICollection<PaymentVoucher> PaymentVouchers { get; set; } = new List<PaymentVoucher>();
}

public class APInvoiceLine
{
    public int Id { get; set; }

    public int APInvoiceId { get; set; }

    public APInvoice? APInvoice { get; set; }

    public int? GLAccountId { get; set; }

    public GLAccount? GLAccount { get; set; }

    public int? InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;

    public decimal UnitCost { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal WithholdingTaxAmount { get; set; }

    public decimal LineTotal { get; set; }
}

public class PaymentVoucher
{
    public int Id { get; set; }

    public string VoucherNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public int? APInvoiceId { get; set; }

    public APInvoice? APInvoice { get; set; }

    public DateTime VoucherDate { get; set; } = DateTime.Today;

    public FinancePaymentMethod PaymentMethod { get; set; } = FinancePaymentMethod.BankTransfer;

    public string? BankAccountName { get; set; }

    public string? BankReferenceNumber { get; set; }

    public string? CheckNumber { get; set; }

    public decimal Amount { get; set; }

    public decimal WithholdingTaxAmount { get; set; }

    public decimal NetPaymentAmount { get; set; }

    public PaymentVoucherStatus Status { get; set; } = PaymentVoucherStatus.Draft;

    public string PreparedBy { get; set; } = string.Empty;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? ReleasedBy { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public string? Notes { get; set; }

    public int? JournalEntryId { get; set; }

    public JournalEntry? JournalEntry { get; set; }

    public ICollection<Disbursement> Disbursements { get; set; } = new List<Disbursement>();
}

public class Disbursement
{
    public int Id { get; set; }

    public string DisbursementNumber { get; set; } = string.Empty;

    public int? PaymentVoucherId { get; set; }

    public PaymentVoucher? PaymentVoucher { get; set; }

    public int? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public DateTime DisbursementDate { get; set; } = DateTime.Today;

    public FinancePaymentMethod PaymentMethod { get; set; } = FinancePaymentMethod.BankTransfer;

    public decimal Amount { get; set; }

    public string? ReferenceNumber { get; set; }

    public string PaidBy { get; set; } = string.Empty;

    public DisbursementStatus Status { get; set; } = DisbursementStatus.Draft;

    public string? Notes { get; set; }

    public int? JournalEntryId { get; set; }

    public JournalEntry? JournalEntry { get; set; }
}

public class BankAccount
{
    public int Id { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string? AccountNumber { get; set; }

    public int? GLAccountId { get; set; }

    public GLAccount? GLAccount { get; set; }

    public string Currency { get; set; } = "PHP";

    public decimal OpeningBalance { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public ICollection<BankTransaction> BankTransactions { get; set; } = new List<BankTransaction>();
}

public class BankTransaction
{
    public int Id { get; set; }

    public int BankAccountId { get; set; }

    public BankAccount? BankAccount { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.Today;

    public string Description { get; set; } = string.Empty;

    public string? ReferenceNumber { get; set; }

    public decimal DebitAmount { get; set; }

    public decimal CreditAmount { get; set; }

    public SourceModule SourceModule { get; set; } = SourceModule.Finance;

    public int? SourceReferenceId { get; set; }

    public int? JournalEntryId { get; set; }

    public JournalEntry? JournalEntry { get; set; }

    public bool IsReconciled { get; set; }

    public DateTime? ReconciledAt { get; set; }

    public string? ReconciledBy { get; set; }

    public string? Notes { get; set; }
}

public class BankReconciliation
{
    public int Id { get; set; }

    public int BankAccountId { get; set; }

    public BankAccount? BankAccount { get; set; }

    public DateTime ReconciliationDate { get; set; } = DateTime.Today;

    public decimal StatementEndingBalance { get; set; }

    public decimal BookEndingBalance { get; set; }

    public decimal Difference { get; set; }

    public BankReconciliationStatus Status { get; set; } = BankReconciliationStatus.Draft;

    public string PreparedBy { get; set; } = string.Empty;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }

    public ICollection<BankReconciliationItem> Items { get; set; } = new List<BankReconciliationItem>();
}

public class BankReconciliationItem
{
    public int Id { get; set; }

    public int BankReconciliationId { get; set; }

    public BankReconciliation? BankReconciliation { get; set; }

    public int? BankTransactionId { get; set; }

    public BankTransaction? BankTransaction { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public BankReconciliationItemType ItemType { get; set; }

    public bool IsCleared { get; set; }

    public string? Notes { get; set; }
}

public class AccrualEntry
{
    public int Id { get; set; }

    public string AccrualNumber { get; set; } = string.Empty;

    public int? AccountingPeriodId { get; set; }

    public AccountingPeriod? AccountingPeriod { get; set; }

    public DateTime AccrualDate { get; set; } = DateTime.Today;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int DebitGLAccountId { get; set; }

    public GLAccount? DebitGLAccount { get; set; }

    public int CreditGLAccountId { get; set; }

    public GLAccount? CreditGLAccount { get; set; }

    public AccrualEntryStatus Status { get; set; } = AccrualEntryStatus.Draft;

    public string CreatedBy { get; set; } = string.Empty;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int? JournalEntryId { get; set; }

    public JournalEntry? JournalEntry { get; set; }

    public int? ReversalJournalEntryId { get; set; }

    public JournalEntry? ReversalJournalEntry { get; set; }

    public string? Notes { get; set; }
}

public class MonthEndCloseChecklist
{
    public int Id { get; set; }

    public int AccountingPeriodId { get; set; }

    public AccountingPeriod? AccountingPeriod { get; set; }

    public string ChecklistItem { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public MonthEndChecklistStatus Status { get; set; } = MonthEndChecklistStatus.Pending;

    public string? CompletedBy { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }
}
