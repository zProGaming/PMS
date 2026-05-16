using Vantage.PMS.Models.Banquet;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Models.Finance;

public enum ChargeCategory
{
    Room = 0,
    FoodBeverage = 1,
    Banquet = 2,
    Miscellaneous = 3,
    Tax = 4,
    ServiceCharge = 5,
    Discount = 6,
    Refund = 7,
    Adjustment = 8
}

public enum CashierShiftStatus
{
    Open = 0,
    Closed = 1,
    Audited = 2,
    Cancelled = 3
}

public enum CashierTransactionType
{
    Payment = 0,
    Refund = 1,
    CashDrop = 2,
    PaidOut = 3,
    Adjustment = 4
}

public enum FinancePaymentMethod
{
    Cash = 0,
    CreditCard = 1,
    DebitCard = 2,
    BankTransfer = 3,
    EWallet = 4,
    CompanyCharge = 5,
    GiftCertificate = 6,
    Other = 7
}

public enum RefundStatus
{
    Requested = 0,
    ForApproval = 1,
    Approved = 2,
    Rejected = 3,
    Processed = 4,
    Cancelled = 5
}

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

public enum FinanceDocumentType
{
    ProFormaInvoice = 0,
    StatementOfAccount = 1,
    OfficialInvoice = 2,
    AcknowledgementReceipt = 3,
    PaymentReceipt = 4,
    CreditMemo = 5,
    DebitMemo = 6,
    ChargeSlip = 7
}

public enum FinanceDocumentStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Voided = 4,
    Cancelled = 5
}

public enum ARInvoiceStatus
{
    Open = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Overdue = 3,
    WrittenOff = 4,
    Cancelled = 5
}

public enum ARAccountType
{
    Corporate = 0,
    TravelAgency = 1,
    Government = 2,
    EventClient = 3,
    OnlineTravelAgency = 4,
    Other = 5
}

public enum MemoStatus
{
    Draft = 0,
    ForApproval = 1,
    Approved = 2,
    Applied = 3,
    Cancelled = 4
}

public class ChargeCode
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ChargeCategory ChargeCategory { get; set; } = ChargeCategory.Miscellaneous;

    public bool IsTaxable { get; set; }

    public bool IsServiceChargeable { get; set; }

    public bool IsActive { get; set; } = true;

    public decimal? DefaultAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;
}

public class CashierShift
{
    public int Id { get; set; }

    public string ShiftNumber { get; set; } = string.Empty;

    public DateTime BusinessDate { get; set; } = DateTime.Today;

    public string OpenedBy { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; } = DateTime.Now;

    public decimal OpeningCashFloat { get; set; }

    public string? ClosedBy { get; set; }

    public DateTime? ClosedAt { get; set; }

    public decimal? ClosingCashCount { get; set; }

    public decimal? ExpectedCashAmount { get; set; }

    public decimal? CashOverShort { get; set; }

    public CashierShiftStatus Status { get; set; } = CashierShiftStatus.Open;

    public string? Notes { get; set; }

    public ICollection<CashierTransaction> Transactions { get; set; } = new List<CashierTransaction>();

    public ICollection<CashDrop> CashDrops { get; set; } = new List<CashDrop>();
}

public class CashierTransaction
{
    public int Id { get; set; }

    public int? CashierShiftId { get; set; }

    public CashierShift? CashierShift { get; set; }

    public int? PaymentId { get; set; }

    public Payment? Payment { get; set; }

    public int? FolioId { get; set; }

    public Folio? Folio { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.Now;

    public CashierTransactionType TransactionType { get; set; }

    public decimal Amount { get; set; }

    public FinancePaymentMethod PaymentMethod { get; set; } = FinancePaymentMethod.Cash;

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public bool IsVoided { get; set; }
}

public class CashDrop
{
    public int Id { get; set; }

    public int CashierShiftId { get; set; }

    public CashierShift? CashierShift { get; set; }

    public DateTime DropDate { get; set; } = DateTime.Now;

    public decimal Amount { get; set; }

    public string DroppedBy { get; set; } = string.Empty;

    public string? ReceivedBy { get; set; }

    public string? Notes { get; set; }
}

public class RefundTransaction
{
    public int Id { get; set; }

    public int? FolioId { get; set; }

    public Folio? Folio { get; set; }

    public int? PaymentId { get; set; }

    public Payment? Payment { get; set; }

    public string RefundNumber { get; set; } = string.Empty;

    public DateTime RefundDate { get; set; } = DateTime.Now;

    public decimal Amount { get; set; }

    public FinancePaymentMethod RefundMethod { get; set; } = FinancePaymentMethod.Cash;

    public string Reason { get; set; } = string.Empty;

    public RefundStatus Status { get; set; } = RefundStatus.Requested;

    public string RequestedBy { get; set; } = string.Empty;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? ProcessedBy { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? Notes { get; set; }
}

public class VoidRequest
{
    public int Id { get; set; }

    public string ReferenceType { get; set; } = string.Empty;

    public int ReferenceId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public string RequestedBy { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; } = DateTime.Now;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? RejectedBy { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? Notes { get; set; }
}

public class DiscountApproval
{
    public int Id { get; set; }

    public int? FolioId { get; set; }

    public Folio? Folio { get; set; }

    public int? FolioItemId { get; set; }

    public FolioItem? FolioItem { get; set; }

    public int? POSOrderId { get; set; }

    public POSOrder? POSOrder { get; set; }

    public int? BanquetEventId { get; set; }

    public BanquetEvent? BanquetEvent { get; set; }

    public DiscountType DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal DiscountAmount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public string RequestedBy { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; } = DateTime.Now;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }
}

public class FinanceDocument
{
    public int Id { get; set; }

    public string DocumentNumber { get; set; } = string.Empty;

    public FinanceDocumentType DocumentType { get; set; }

    public int? FolioId { get; set; }

    public Folio? Folio { get; set; }

    public int? ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int? GuestId { get; set; }

    public Guest? Guest { get; set; }

    public int? SalesAccountId { get; set; }

    public SalesAccount? SalesAccount { get; set; }

    public int? BanquetEventId { get; set; }

    public BanquetEvent? BanquetEvent { get; set; }

    public DateTime DocumentDate { get; set; } = DateTime.Today;

    public DateTime? DueDate { get; set; }

    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal ServiceCharge { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Balance { get; set; }

    public FinanceDocumentStatus Status { get; set; } = FinanceDocumentStatus.Draft;

    public string BillingName { get; set; } = string.Empty;

    public string? BillingAddress { get; set; }

    public string? BillingTIN { get; set; }

    public string? Notes { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? VoidedBy { get; set; }

    public DateTime? VoidedAt { get; set; }

    public string? VoidReason { get; set; }

    public ICollection<FinanceDocumentLine> Lines { get; set; } = new List<FinanceDocumentLine>();
}

public class FinanceDocumentLine
{
    public int Id { get; set; }

    public int FinanceDocumentId { get; set; }

    public FinanceDocument? FinanceDocument { get; set; }

    public int? ChargeCodeId { get; set; }

    public ChargeCode? ChargeCode { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal ServiceCharge { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }
}

public class ARAccount
{
    public int Id { get; set; }

    public int? SalesAccountId { get; set; }

    public SalesAccount? SalesAccount { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public ARAccountType AccountType { get; set; } = ARAccountType.Corporate;

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? BillingAddress { get; set; }

    public decimal CreditLimit { get; set; }

    public decimal CurrentBalance { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public ICollection<ARInvoice> Invoices { get; set; } = new List<ARInvoice>();

    public ICollection<ARPayment> Payments { get; set; } = new List<ARPayment>();
}

public class ARInvoice
{
    public int Id { get; set; }

    public int ARAccountId { get; set; }

    public ARAccount? ARAccount { get; set; }

    public int? FinanceDocumentId { get; set; }

    public FinanceDocument? FinanceDocument { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

    public decimal OriginalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Balance { get; set; }

    public ARInvoiceStatus Status { get; set; } = ARInvoiceStatus.Open;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<ARPaymentAllocation> Allocations { get; set; } = new List<ARPaymentAllocation>();
}

public class ARPayment
{
    public int Id { get; set; }

    public int ARAccountId { get; set; }

    public ARAccount? ARAccount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Today;

    public decimal Amount { get; set; }

    public FinancePaymentMethod PaymentMethod { get; set; } = FinancePaymentMethod.Cash;

    public string? ReferenceNumber { get; set; }

    public string ReceivedBy { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public ICollection<ARPaymentAllocation> Allocations { get; set; } = new List<ARPaymentAllocation>();

    public decimal AllocatedAmount => Allocations.Sum(allocation => allocation.AllocatedAmount);

    public decimal RemainingAmount => Amount - AllocatedAmount;
}

public class ARPaymentAllocation
{
    public int Id { get; set; }

    public int ARPaymentId { get; set; }

    public ARPayment? ARPayment { get; set; }

    public int ARInvoiceId { get; set; }

    public ARInvoice? ARInvoice { get; set; }

    public decimal AllocatedAmount { get; set; }

    public DateTime AllocationDate { get; set; } = DateTime.Now;

    public string AllocatedBy { get; set; } = string.Empty;
}

public class CreditMemo
{
    public int Id { get; set; }

    public int? ARAccountId { get; set; }

    public ARAccount? ARAccount { get; set; }

    public int? ARInvoiceId { get; set; }

    public ARInvoice? ARInvoice { get; set; }

    public int? FinanceDocumentId { get; set; }

    public FinanceDocument? FinanceDocument { get; set; }

    public string CreditMemoNumber { get; set; } = string.Empty;

    public DateTime CreditMemoDate { get; set; } = DateTime.Today;

    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public MemoStatus Status { get; set; } = MemoStatus.Draft;

    public string CreatedBy { get; set; } = string.Empty;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? AppliedAt { get; set; }

    public string? Notes { get; set; }
}

public class DebitMemo
{
    public int Id { get; set; }

    public int? ARAccountId { get; set; }

    public ARAccount? ARAccount { get; set; }

    public int? ARInvoiceId { get; set; }

    public ARInvoice? ARInvoice { get; set; }

    public int? FinanceDocumentId { get; set; }

    public FinanceDocument? FinanceDocument { get; set; }

    public string DebitMemoNumber { get; set; } = string.Empty;

    public DateTime DebitMemoDate { get; set; } = DateTime.Today;

    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public MemoStatus Status { get; set; } = MemoStatus.Draft;

    public string CreatedBy { get; set; } = string.Empty;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? AppliedAt { get; set; }

    public string? Notes { get; set; }
}

public class DocumentNumberSequence
{
    public int Id { get; set; }

    public FinanceDocumentType DocumentType { get; set; }

    public string Prefix { get; set; } = string.Empty;

    public int NextNumber { get; set; } = 1;

    public int PaddingLength { get; set; } = 6;

    public bool IsActive { get; set; } = true;
}
