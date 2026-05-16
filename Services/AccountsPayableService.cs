using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public class AccountsPayableService(ApplicationDbContext context, AccountingPostingService postingService, AuditLogService auditLogService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly AccountingPostingService _postingService = postingService;
    private readonly AuditLogService _auditLogService = auditLogService;

    private static readonly string[] MonthEndChecklistDefaults =
    [
        "All cashier shifts closed",
        "Night audits completed",
        "Folio charges posted",
        "Payments posted",
        "POS transactions posted",
        "Banquet transactions posted",
        "Receiving records posted",
        "AP invoices reviewed",
        "Payment vouchers reviewed",
        "Bank reconciliation completed",
        "AR aging reviewed",
        "Trial balance balanced",
        "Accruals posted",
        "USALI report reviewed",
        "Philippine finance reports reviewed"
    ];

    public void RecalculateAPInvoice(APInvoice invoice)
    {
        foreach (var line in invoice.Lines)
        {
            line.LineTotal = (line.Quantity * line.UnitCost) + line.TaxAmount - line.WithholdingTaxAmount;
        }

        invoice.SubTotal = invoice.Lines.Sum(line => line.Quantity * line.UnitCost);
        invoice.TaxAmount = invoice.Lines.Sum(line => line.TaxAmount);
        invoice.WithholdingTaxAmount = invoice.Lines.Sum(line => line.WithholdingTaxAmount);
        invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount - invoice.DiscountAmount;
        invoice.Balance = Math.Max(0, invoice.TotalAmount - invoice.WithholdingTaxAmount - invoice.AmountPaid);
    }

    public async Task<string> GenerateNumberAsync(string prefix)
    {
        var todayPrefix = $"{prefix}-{DateTime.Today:yyyyMMdd}";
        var existingCount = prefix switch
        {
            "APINV" => await _context.APInvoices.CountAsync(item => item.InvoiceNumber.StartsWith(todayPrefix)),
            "PV" => await _context.PaymentVouchers.CountAsync(item => item.VoucherNumber.StartsWith(todayPrefix)),
            "DISB" => await _context.Disbursements.CountAsync(item => item.DisbursementNumber.StartsWith(todayPrefix)),
            "ACCR" => await _context.AccrualEntries.CountAsync(item => item.AccrualNumber.StartsWith(todayPrefix)),
            _ => 0
        };

        return $"{todayPrefix}-{existingCount + 1:0000}";
    }

    public async Task<APInvoice> BuildInvoiceFromPurchaseOrderAsync(int purchaseOrderId, string createdBy)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.InventoryItem)
            .FirstAsync(order => order.Id == purchaseOrderId);

        var invoice = new APInvoice
        {
            SupplierId = purchaseOrder.SupplierId,
            PurchaseOrderId = purchaseOrder.Id,
            InvoiceNumber = await GenerateNumberAsync("APINV"),
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            CreatedAt = DateTime.Now,
            CreatedBy = createdBy,
            Notes = $"Created from PO {purchaseOrder.PONumber}.",
            Lines = purchaseOrder.Items.Select(item => new APInvoiceLine
            {
                InventoryItemId = item.InventoryItemId,
                Description = item.InventoryItem?.ItemName ?? item.Notes ?? "Purchase order line",
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                TaxAmount = 0,
                WithholdingTaxAmount = 0
            }).ToList()
        };

        RecalculateAPInvoice(invoice);
        return invoice;
    }

    public async Task<APInvoice> BuildInvoiceFromReceivingRecordAsync(int receivingRecordId, string createdBy)
    {
        var receiving = await _context.ReceivingRecords
            .AsNoTracking()
            .Include(record => record.PurchaseOrder)
            .Include(record => record.Items)
            .ThenInclude(item => item.InventoryItem)
            .FirstAsync(record => record.Id == receivingRecordId);
        var supplierId = receiving.SupplierId ?? receiving.PurchaseOrder?.SupplierId;
        if (supplierId is null)
        {
            throw new InvalidOperationException("Receiving record has no supplier or linked purchase order supplier.");
        }

        var invoice = new APInvoice
        {
            SupplierId = supplierId.Value,
            PurchaseOrderId = receiving.PurchaseOrderId,
            ReceivingRecordId = receiving.Id,
            InvoiceNumber = await GenerateNumberAsync("APINV"),
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            CreatedAt = DateTime.Now,
            CreatedBy = createdBy,
            Notes = $"Created from receiving {receiving.ReceivingNumber}.",
            Lines = receiving.Items.Select(item => new APInvoiceLine
            {
                InventoryItemId = item.InventoryItemId,
                Description = item.InventoryItem?.ItemName ?? item.Notes ?? "Receiving line",
                Quantity = item.QuantityReceived,
                UnitCost = item.UnitCost,
                TaxAmount = 0,
                WithholdingTaxAmount = 0
            }).ToList()
        };

        RecalculateAPInvoice(invoice);
        return invoice;
    }

    public async Task<IList<string>> ApproveAPInvoiceAsync(int apInvoiceId, string approvedBy)
    {
        var errors = new List<string>();
        var invoice = await _context.APInvoices
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == apInvoiceId);

        if (invoice is null)
        {
            errors.Add("AP invoice was not found.");
            return errors;
        }

        if (invoice.Status is not (APInvoiceStatus.Draft or APInvoiceStatus.ForApproval))
        {
            errors.Add("Only draft or for-approval AP invoices can be approved.");
            return errors;
        }

        if (await _context.APInvoices.AnyAsync(item => item.Id != invoice.Id && item.SupplierId == invoice.SupplierId && item.InvoiceNumber == invoice.InvoiceNumber))
        {
            errors.Add("Invoice number already exists for this supplier.");
        }

        RecalculateAPInvoice(invoice);
        if (invoice.Lines.Count == 0 || invoice.TotalAmount <= 0)
        {
            errors.Add("AP invoice must have lines and a positive total.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var alreadyPosted = await _context.JournalEntries.AnyAsync(entry =>
            entry.Status == JournalEntryStatus.Posted &&
            entry.SourceModule == SourceModule.Purchasing &&
            entry.SourceTransactionType == SourceTransactionType.APInvoice &&
            entry.SourceReferenceId == invoice.Id);
        if (alreadyPosted)
        {
            errors.Add("This AP invoice already has a posted journal entry.");
            return errors;
        }

        var journalEntry = await CreateAPInvoiceJournalEntryAsync(invoice, approvedBy);
        var postErrors = await _postingService.PostJournalEntryAsync(journalEntry.Id, approvedBy);
        if (postErrors.Count > 0)
        {
            errors.AddRange(postErrors);
            return errors;
        }

        invoice.JournalEntryId = journalEntry.Id;
        invoice.Status = APInvoiceStatus.Approved;
        invoice.ApprovedBy = approvedBy;
        invoice.ApprovedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(AuditActionType.Approve, "Accounts Payable", nameof(APInvoice), invoice.Id.ToString(), null, new { invoice.InvoiceNumber, invoice.TotalAmount, invoice.Balance, invoice.JournalEntryId }, approvedBy);

        return errors;
    }

    public async Task<IList<string>> ApprovePaymentVoucherAsync(int paymentVoucherId, string approvedBy)
    {
        var errors = new List<string>();
        var voucher = await _context.PaymentVouchers
            .Include(item => item.APInvoice)
            .FirstOrDefaultAsync(item => item.Id == paymentVoucherId);
        if (voucher is null)
        {
            errors.Add("Payment voucher was not found.");
            return errors;
        }

        if (voucher.Status is not (PaymentVoucherStatus.Draft or PaymentVoucherStatus.ForApproval))
        {
            errors.Add("Only draft or for-approval vouchers can be approved.");
            return errors;
        }

        if (voucher.Amount <= 0)
        {
            errors.Add("Voucher amount must be greater than zero.");
        }

        if (voucher.APInvoice is not null && voucher.Amount > voucher.APInvoice.Balance)
        {
            errors.Add("Voucher amount cannot exceed AP invoice balance.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        voucher.NetPaymentAmount = voucher.Amount - voucher.WithholdingTaxAmount;
        voucher.Status = PaymentVoucherStatus.Approved;
        voucher.ApprovedBy = approvedBy;
        voucher.ApprovedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(AuditActionType.Approve, "Accounts Payable", nameof(PaymentVoucher), voucher.Id.ToString(), null, new { voucher.VoucherNumber, voucher.Amount, voucher.NetPaymentAmount }, approvedBy);
        return errors;
    }

    public async Task<IList<string>> ReleasePaymentVoucherAsync(int paymentVoucherId, int? bankAccountId, string releasedBy)
    {
        var errors = new List<string>();
        var voucher = await _context.PaymentVouchers
            .Include(item => item.APInvoice)
            .FirstOrDefaultAsync(item => item.Id == paymentVoucherId);

        if (voucher is null)
        {
            errors.Add("Payment voucher was not found.");
            return errors;
        }

        if (voucher.Status != PaymentVoucherStatus.Approved)
        {
            errors.Add("Only approved payment vouchers can be released.");
            return errors;
        }

        voucher.NetPaymentAmount = voucher.Amount - voucher.WithholdingTaxAmount;
        if (voucher.NetPaymentAmount < 0)
        {
            errors.Add("Net payment cannot be negative.");
            return errors;
        }

        var alreadyPosted = await _context.JournalEntries.AnyAsync(entry =>
            entry.Status == JournalEntryStatus.Posted &&
            entry.SourceModule == SourceModule.Finance &&
            entry.SourceTransactionType == SourceTransactionType.PaymentVoucher &&
            entry.SourceReferenceId == voucher.Id);
        if (alreadyPosted)
        {
            errors.Add("This payment voucher already has a posted journal entry.");
            return errors;
        }

        var journalEntry = await CreatePaymentVoucherJournalEntryAsync(voucher, bankAccountId, releasedBy);
        var postErrors = await _postingService.PostJournalEntryAsync(journalEntry.Id, releasedBy);
        if (postErrors.Count > 0)
        {
            errors.AddRange(postErrors);
            return errors;
        }

        voucher.JournalEntryId = journalEntry.Id;
        voucher.Status = PaymentVoucherStatus.Released;
        voucher.ReleasedBy = releasedBy;
        voucher.ReleasedAt = DateTime.Now;

        if (voucher.APInvoice is not null)
        {
            voucher.APInvoice.AmountPaid += voucher.Amount;
            voucher.APInvoice.Balance = Math.Max(0, voucher.APInvoice.Balance - voucher.Amount);
            voucher.APInvoice.Status = voucher.APInvoice.Balance == 0 ? APInvoiceStatus.Paid : APInvoiceStatus.PartiallyPaid;
        }

        var disbursement = new Disbursement
        {
            DisbursementNumber = await GenerateNumberAsync("DISB"),
            PaymentVoucherId = voucher.Id,
            SupplierId = voucher.SupplierId,
            DisbursementDate = voucher.VoucherDate,
            PaymentMethod = voucher.PaymentMethod,
            Amount = voucher.NetPaymentAmount,
            ReferenceNumber = voucher.BankReferenceNumber ?? voucher.CheckNumber,
            PaidBy = releasedBy,
            Status = DisbursementStatus.Released,
            JournalEntryId = journalEntry.Id,
            Notes = $"Released from voucher {voucher.VoucherNumber}."
        };
        _context.Disbursements.Add(disbursement);

        if (bankAccountId is not null && voucher.PaymentMethod is FinancePaymentMethod.BankTransfer or FinancePaymentMethod.DebitCard or FinancePaymentMethod.CreditCard or FinancePaymentMethod.EWallet)
        {
            _context.BankTransactions.Add(new BankTransaction
            {
                BankAccountId = bankAccountId.Value,
                TransactionDate = voucher.VoucherDate,
                Description = $"Payment voucher {voucher.VoucherNumber}",
                ReferenceNumber = voucher.BankReferenceNumber ?? voucher.CheckNumber,
                DebitAmount = 0,
                CreditAmount = voucher.NetPaymentAmount,
                SourceModule = SourceModule.Finance,
                SourceReferenceId = voucher.Id,
                Notes = voucher.Notes
            });
        }

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(AuditActionType.Post, "Accounts Payable", nameof(PaymentVoucher), voucher.Id.ToString(), null, new { voucher.VoucherNumber, voucher.Amount, voucher.NetPaymentAmount, voucher.JournalEntryId }, releasedBy);

        return errors;
    }

    public async Task<IList<string>> ApproveBankReconciliationAsync(int bankReconciliationId, string approvedBy)
    {
        var errors = new List<string>();
        var reconciliation = await _context.BankReconciliations
            .Include(item => item.BankAccount)
            .Include(item => item.Items)
            .ThenInclude(item => item.BankTransaction)
            .FirstOrDefaultAsync(item => item.Id == bankReconciliationId);

        if (reconciliation is null)
        {
            errors.Add("Bank reconciliation was not found.");
            return errors;
        }

        RecalculateBankReconciliation(reconciliation);
        if (reconciliation.Difference != 0)
        {
            errors.Add("Difference must be zero before bank reconciliation can be approved.");
            return errors;
        }

        reconciliation.Status = BankReconciliationStatus.Approved;
        reconciliation.ApprovedBy = approvedBy;
        reconciliation.ApprovedAt = DateTime.Now;

        var transactionIds = reconciliation.Items
            .Where(item => item.IsCleared && item.BankTransactionId is not null)
            .Select(item => item.BankTransactionId!.Value)
            .ToList();
        var transactions = await _context.BankTransactions.Where(item => transactionIds.Contains(item.Id)).ToListAsync();
        foreach (var transaction in transactions)
        {
            transaction.IsReconciled = true;
            transaction.ReconciledAt = DateTime.Now;
            transaction.ReconciledBy = approvedBy;
        }

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(AuditActionType.Approve, "Banking", nameof(BankReconciliation), reconciliation.Id.ToString(), null, new { reconciliation.ReconciliationDate, reconciliation.StatementEndingBalance, reconciliation.BookEndingBalance, reconciliation.Difference }, approvedBy);
        return errors;
    }

    public void RecalculateBankReconciliation(BankReconciliation reconciliation)
    {
        var openingBalance = reconciliation.BankAccount?.OpeningBalance ?? 0;
        var clearedAdjustments = reconciliation.Items
            .Where(item => item.BankTransactionId is null && item.IsCleared)
            .Sum(item => item.Amount);
        var clearedTransactions = reconciliation.Items
            .Where(item => item.BankTransaction is not null && item.IsCleared)
            .Sum(item => (item.BankTransaction!.DebitAmount - item.BankTransaction.CreditAmount));

        reconciliation.BookEndingBalance = openingBalance + clearedTransactions + clearedAdjustments;
        reconciliation.Difference = reconciliation.StatementEndingBalance - reconciliation.BookEndingBalance;
        if (reconciliation.Difference == 0 && reconciliation.Status == BankReconciliationStatus.Draft)
        {
            reconciliation.Status = BankReconciliationStatus.Balanced;
        }
    }

    public async Task<IList<string>> ApproveAccrualAsync(int accrualId, string approvedBy)
    {
        var errors = new List<string>();
        var accrual = await _context.AccrualEntries.FindAsync(accrualId);
        if (accrual is null)
        {
            errors.Add("Accrual entry was not found.");
            return errors;
        }

        if (accrual.Amount <= 0 || accrual.DebitGLAccountId == accrual.CreditGLAccountId)
        {
            errors.Add("Accrual amount must be positive and debit/credit accounts must be different.");
            return errors;
        }

        accrual.Status = AccrualEntryStatus.Approved;
        accrual.ApprovedBy = approvedBy;
        accrual.ApprovedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(AuditActionType.Approve, "Month-End", nameof(AccrualEntry), accrual.Id.ToString(), null, new { accrual.AccrualNumber, accrual.Amount }, approvedBy);
        return errors;
    }

    public async Task<IList<string>> ReverseAccrualAsync(int accrualId, string reversedBy)
    {
        var errors = new List<string>();
        var accrual = await _context.AccrualEntries.FindAsync(accrualId);
        if (accrual is null)
        {
            errors.Add("Accrual entry was not found.");
            return errors;
        }

        if (accrual.Status != AccrualEntryStatus.Posted || accrual.JournalEntryId is null)
        {
            errors.Add("Only posted accruals can be reversed.");
            return errors;
        }

        var journal = new JournalEntry
        {
            JournalNumber = await GenerateJournalNumberAsync("ACCRREV"),
            JournalDate = DateTime.Today,
            AccountingPeriodId = await FindOpenPeriodIdAsync(DateTime.Today),
            SourceModule = SourceModule.Finance,
            SourceTransactionType = SourceTransactionType.Accrual,
            SourceReferenceId = accrual.Id,
            SourceReferenceNumber = accrual.AccrualNumber,
            Description = $"Reversal of accrual {accrual.AccrualNumber}: {accrual.Description}",
            Status = JournalEntryStatus.Draft,
            CreatedBy = reversedBy,
            Lines =
            {
                new JournalEntryLine { GLAccountId = accrual.CreditGLAccountId, DebitAmount = accrual.Amount, Description = $"Reversal - {accrual.Description}" },
                new JournalEntryLine { GLAccountId = accrual.DebitGLAccountId, CreditAmount = accrual.Amount, Description = $"Reversal - {accrual.Description}" }
            }
        };

        _context.JournalEntries.Add(journal);
        await _context.SaveChangesAsync();
        var postErrors = await _postingService.PostJournalEntryAsync(journal.Id, reversedBy);
        if (postErrors.Count > 0)
        {
            errors.AddRange(postErrors);
            return errors;
        }

        accrual.ReversalJournalEntryId = journal.Id;
        accrual.Status = AccrualEntryStatus.Reversed;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(AuditActionType.Other, "Month-End", nameof(AccrualEntry), accrual.Id.ToString(), null, new { accrual.AccrualNumber, accrual.ReversalJournalEntryId }, reversedBy);
        return errors;
    }

    public async Task<IList<string>> PostAccrualAsync(int accrualId, string postedBy)
    {
        var errors = new List<string>();
        var accrual = await _context.AccrualEntries.FindAsync(accrualId);
        if (accrual is null)
        {
            errors.Add("Accrual entry was not found.");
            return errors;
        }

        if (accrual.Status != AccrualEntryStatus.Approved)
        {
            errors.Add("Only approved accruals can be posted.");
            return errors;
        }

        var journal = new JournalEntry
        {
            JournalNumber = await GenerateJournalNumberAsync("ACCRJE"),
            JournalDate = accrual.AccrualDate,
            AccountingPeriodId = accrual.AccountingPeriodId,
            SourceModule = SourceModule.Finance,
            SourceTransactionType = SourceTransactionType.Accrual,
            SourceReferenceId = accrual.Id,
            SourceReferenceNumber = accrual.AccrualNumber,
            Description = accrual.Description,
            Status = JournalEntryStatus.Draft,
            CreatedBy = postedBy,
            Lines =
            {
                new JournalEntryLine { GLAccountId = accrual.DebitGLAccountId, DebitAmount = accrual.Amount, Description = accrual.Description },
                new JournalEntryLine { GLAccountId = accrual.CreditGLAccountId, CreditAmount = accrual.Amount, Description = accrual.Description }
            }
        };

        _context.JournalEntries.Add(journal);
        await _context.SaveChangesAsync();
        var postErrors = await _postingService.PostJournalEntryAsync(journal.Id, postedBy);
        if (postErrors.Count == 0)
        {
            accrual.JournalEntryId = journal.Id;
            accrual.Status = AccrualEntryStatus.Posted;
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(AuditActionType.Post, "Month-End", nameof(AccrualEntry), accrual.Id.ToString(), null, new { accrual.AccrualNumber, accrual.Amount, accrual.JournalEntryId }, postedBy);
        }
        else
        {
            errors.AddRange(postErrors);
        }

        return errors;
    }

    public async Task EnsureMonthEndChecklistAsync(int accountingPeriodId)
    {
        var existingItems = await _context.MonthEndCloseChecklists
            .Where(item => item.AccountingPeriodId == accountingPeriodId)
            .Select(item => item.ChecklistItem)
            .ToListAsync();

        foreach (var item in MonthEndChecklistDefaults.Where(item => !existingItems.Contains(item)))
        {
            _context.MonthEndCloseChecklists.Add(new MonthEndCloseChecklist
            {
                AccountingPeriodId = accountingPeriodId,
                ChecklistItem = item,
                Module = ResolveChecklistModule(item),
                Status = MonthEndChecklistStatus.Pending
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IList<string>> CloseAccountingPeriodAsync(int accountingPeriodId, string closedBy, bool overrideIncomplete)
    {
        var errors = new List<string>();
        await EnsureMonthEndChecklistAsync(accountingPeriodId);
        var period = await _context.AccountingPeriods.FindAsync(accountingPeriodId);
        if (period is null)
        {
            errors.Add("Accounting period was not found.");
            return errors;
        }

        var checklist = await _context.MonthEndCloseChecklists.Where(item => item.AccountingPeriodId == accountingPeriodId).ToListAsync();
        var complete = checklist.All(item => item.Status is MonthEndChecklistStatus.Completed or MonthEndChecklistStatus.NotApplicable);
        if (!complete && !overrideIncomplete)
        {
            errors.Add("Month-end checklist must be complete before closing the accounting period.");
            return errors;
        }

        period.Status = AccountingPeriodStatus.Closed;
        period.ClosedBy = closedBy;
        period.ClosedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(AuditActionType.Approve, "Month-End", nameof(AccountingPeriod), period.Id.ToString(), null, new { period.PeriodName, period.Status, period.ClosedAt }, closedBy);
        return errors;
    }

    private async Task<JournalEntry> CreateAPInvoiceJournalEntryAsync(APInvoice invoice, string createdBy)
    {
        var apAccountId = await GetRequiredAccountIdAsync("2000");
        var inputVatAccountId = await GetRequiredAccountIdAsync("2020");
        var whtAccountId = await GetRequiredAccountIdAsync("2030");
        var defaultExpenseAccountId = await GetRequiredAccountIdAsync("6200");
        var inventoryAccountId = await GetRequiredAccountIdAsync("1200");
        var journal = new JournalEntry
        {
            JournalNumber = await GenerateJournalNumberAsync("APJE"),
            JournalDate = invoice.InvoiceDate,
            AccountingPeriodId = await FindOpenPeriodIdAsync(invoice.InvoiceDate),
            SourceModule = SourceModule.Purchasing,
            SourceTransactionType = SourceTransactionType.APInvoice,
            SourceReferenceId = invoice.Id,
            SourceReferenceNumber = invoice.InvoiceNumber,
            Description = $"AP invoice {invoice.InvoiceNumber}",
            Status = JournalEntryStatus.Draft,
            CreatedBy = createdBy
        };

        foreach (var line in invoice.Lines)
        {
            var baseAmount = line.Quantity * line.UnitCost;
            journal.Lines.Add(new JournalEntryLine
            {
                GLAccountId = line.GLAccountId ?? (line.InventoryItemId is not null ? inventoryAccountId : defaultExpenseAccountId),
                DebitAmount = baseAmount,
                Description = line.Description,
                LineReferenceType = nameof(APInvoiceLine),
                LineReferenceId = line.Id
            });
        }

        if (invoice.TaxAmount > 0)
        {
            journal.Lines.Add(new JournalEntryLine { GLAccountId = inputVatAccountId, DebitAmount = invoice.TaxAmount, Description = $"Input VAT - {invoice.InvoiceNumber}" });
        }

        if (invoice.DiscountAmount > 0)
        {
            journal.Lines.Add(new JournalEntryLine { GLAccountId = defaultExpenseAccountId, CreditAmount = invoice.DiscountAmount, Description = $"Purchase discount - {invoice.InvoiceNumber}" });
        }

        var apCredit = invoice.TotalAmount - invoice.WithholdingTaxAmount;
        if (apCredit > 0)
        {
            journal.Lines.Add(new JournalEntryLine { GLAccountId = apAccountId, CreditAmount = apCredit, Description = $"Accounts payable - {invoice.InvoiceNumber}" });
        }

        if (invoice.WithholdingTaxAmount > 0)
        {
            journal.Lines.Add(new JournalEntryLine { GLAccountId = whtAccountId, CreditAmount = invoice.WithholdingTaxAmount, Description = $"Withholding tax - {invoice.InvoiceNumber}" });
        }

        _context.JournalEntries.Add(journal);
        await _context.SaveChangesAsync();
        return journal;
    }

    private async Task<JournalEntry> CreatePaymentVoucherJournalEntryAsync(PaymentVoucher voucher, int? bankAccountId, string createdBy)
    {
        var apAccountId = await GetRequiredAccountIdAsync("2000");
        var whtAccountId = await GetRequiredAccountIdAsync("2030");
        var cashAccountId = await ResolveCashAccountIdAsync(voucher.PaymentMethod, bankAccountId);
        var journal = new JournalEntry
        {
            JournalNumber = await GenerateJournalNumberAsync("PVJE"),
            JournalDate = voucher.VoucherDate,
            AccountingPeriodId = await FindOpenPeriodIdAsync(voucher.VoucherDate),
            SourceModule = SourceModule.Finance,
            SourceTransactionType = SourceTransactionType.PaymentVoucher,
            SourceReferenceId = voucher.Id,
            SourceReferenceNumber = voucher.VoucherNumber,
            Description = $"Payment voucher {voucher.VoucherNumber}",
            Status = JournalEntryStatus.Draft,
            CreatedBy = createdBy,
            Lines =
            {
                new JournalEntryLine { GLAccountId = apAccountId, DebitAmount = voucher.Amount, Description = $"AP payment - {voucher.VoucherNumber}" },
                new JournalEntryLine { GLAccountId = cashAccountId, CreditAmount = voucher.NetPaymentAmount, Description = $"Cash/bank payment - {voucher.VoucherNumber}" }
            }
        };

        if (voucher.WithholdingTaxAmount > 0)
        {
            journal.Lines.Add(new JournalEntryLine { GLAccountId = whtAccountId, CreditAmount = voucher.WithholdingTaxAmount, Description = $"Withholding tax - {voucher.VoucherNumber}" });
        }

        _context.JournalEntries.Add(journal);
        await _context.SaveChangesAsync();
        return journal;
    }

    private async Task<int> ResolveCashAccountIdAsync(FinancePaymentMethod paymentMethod, int? bankAccountId)
    {
        if (bankAccountId is not null)
        {
            var bankAccountGl = await _context.BankAccounts
                .Where(account => account.Id == bankAccountId && account.GLAccountId != null)
                .Select(account => account.GLAccountId)
                .FirstOrDefaultAsync();
            if (bankAccountGl is not null)
            {
                return bankAccountGl.Value;
            }
        }

        return paymentMethod switch
        {
            FinancePaymentMethod.Cash => await GetRequiredAccountIdAsync("1000"),
            FinancePaymentMethod.EWallet => await GetRequiredAccountIdAsync("1020"),
            _ => await GetRequiredAccountIdAsync("1010")
        };
    }

    private async Task<int> GetRequiredAccountIdAsync(string accountCode)
    {
        var id = await _context.GLAccounts.Where(account => account.AccountCode == accountCode).Select(account => (int?)account.Id).FirstOrDefaultAsync();
        if (id is null)
        {
            throw new InvalidOperationException($"Required GL account {accountCode} was not found.");
        }

        return id.Value;
    }

    private async Task<int?> FindOpenPeriodIdAsync(DateTime date)
    {
        return await _context.AccountingPeriods
            .Where(period => period.Status == AccountingPeriodStatus.Open && period.StartDate <= date.Date && period.EndDate >= date.Date)
            .Select(period => (int?)period.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<string> GenerateJournalNumberAsync(string prefix)
    {
        var todayPrefix = $"{prefix}-{DateTime.Today:yyyyMMdd}";
        var existingCount = await _context.JournalEntries.CountAsync(entry => entry.JournalNumber.StartsWith(todayPrefix));
        return $"{todayPrefix}-{existingCount + 1:0000}";
    }

    private static string ResolveChecklistModule(string checklistItem)
    {
        if (checklistItem.Contains("cashier", StringComparison.OrdinalIgnoreCase) || checklistItem.Contains("voucher", StringComparison.OrdinalIgnoreCase) || checklistItem.Contains("AP", StringComparison.OrdinalIgnoreCase))
        {
            return "Finance";
        }

        if (checklistItem.Contains("POS", StringComparison.OrdinalIgnoreCase))
        {
            return "F&B";
        }

        if (checklistItem.Contains("AR", StringComparison.OrdinalIgnoreCase))
        {
            return "Accounts Receivable";
        }

        if (checklistItem.Contains("report", StringComparison.OrdinalIgnoreCase) || checklistItem.Contains("trial", StringComparison.OrdinalIgnoreCase))
        {
            return "Accounting";
        }

        return "Operations";
    }
}
