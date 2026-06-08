using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Services;

public class FinanceService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<string> GenerateSimpleNumberAsync(string prefix)
    {
        var todayPrefix = $"{prefix}-{DateTime.Today:yyyyMMdd}";
        var existingCount = prefix switch
        {
            "SHIFT" => await _context.CashierShifts.CountAsync(item => item.ShiftNumber.StartsWith(todayPrefix)),
            "REF" => await _context.RefundTransactions.CountAsync(item => item.RefundNumber.StartsWith(todayPrefix)),
            "CM" => await _context.CreditMemos.CountAsync(item => item.CreditMemoNumber.StartsWith(todayPrefix)),
            "DM" => await _context.DebitMemos.CountAsync(item => item.DebitMemoNumber.StartsWith(todayPrefix)),
            "ARINV" => await _context.ARInvoices.CountAsync(item => item.InvoiceNumber.StartsWith(todayPrefix)),
            _ => 0
        };

        return $"{todayPrefix}-{existingCount + 1:0000}";
    }

    public async Task<string> GenerateDocumentNumberAsync(FinanceDocumentType documentType)
    {
        var sequence = await _context.DocumentNumberSequences
            .FirstOrDefaultAsync(item => item.DocumentType == documentType && item.IsActive);

        if (sequence is null)
        {
            sequence = new DocumentNumberSequence
            {
                DocumentType = documentType,
                Prefix = documentType switch
                {
                    FinanceDocumentType.OfficialInvoice => "OI",
                    FinanceDocumentType.AcknowledgementReceipt => "AR",
                    FinanceDocumentType.PaymentReceipt => "PR",
                    FinanceDocumentType.CreditMemo => "CM",
                    FinanceDocumentType.DebitMemo => "DM",
                    FinanceDocumentType.ChargeSlip => "CS",
                    FinanceDocumentType.StatementOfAccount => "SOA",
                    _ => "PF"
                },
                NextNumber = 1,
                PaddingLength = 6,
                IsActive = true
            };
            _context.DocumentNumberSequences.Add(sequence);
        }

        var documentNumber = $"{sequence.Prefix}-{sequence.NextNumber.ToString().PadLeft(sequence.PaddingLength, '0')}";
        sequence.NextNumber++;
        await _context.SaveChangesAsync();
        return documentNumber;
    }

    public async Task<CashierShift?> GetOpenShiftForUserAsync(string userName)
    {
        return await _context.CashierShifts
            .Include(shift => shift.Transactions)
            .Include(shift => shift.CashDrops)
            .FirstOrDefaultAsync(shift => shift.OpenedBy == userName && shift.Status == CashierShiftStatus.Open);
    }

    public async Task<IList<string>> PostFolioPaymentAsync(Payment payment, string createdBy, bool allowWithoutOpenShift)
    {
        var errors = new List<string>();
        if (payment.Amount <= 0)
        {
            errors.Add("Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(payment.PaymentMethod))
        {
            errors.Add("Payment method is required.");
        }

        var folio = await _context.Folios
            .AsNoTracking()
            .Include(item => item.Reservation)
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .FirstOrDefaultAsync(item => item.Id == payment.FolioId);

        if (folio is null)
        {
            errors.Add("Folio was not found.");
        }
        else
        {
            if (folio.Status != FolioStatus.Open)
            {
                errors.Add($"Payments cannot be posted to a {FormatFolioStatus(folio.Status)} folio. Reopen or transfer the folio through an authorized finance workflow before posting.");
            }

            if (payment.Amount > folio.Balance)
            {
                errors.Add($"Payment cannot exceed the open folio balance of {folio.Balance:C}.");
            }

            var normalizedPaymentMethod = NormalizePaymentMethod(payment.PaymentMethod);
            var normalizedReference = NormalizeReference(payment.ReferenceNumber);
            if (!string.IsNullOrWhiteSpace(normalizedReference))
            {
                var duplicateReferenceExists = await _context.Payments
                    .AsNoTracking()
                    .AnyAsync(existing =>
                        existing.FolioId == payment.FolioId &&
                        existing.Status != PaymentStatus.Voided &&
                        existing.Status != PaymentStatus.Failed &&
                        existing.ReferenceNumber != null &&
                        existing.ReferenceNumber.Trim().ToUpper() == normalizedReference);

                if (duplicateReferenceExists)
                {
                    errors.Add("A payment with the same reference number already exists on this folio. Review the existing receipt before posting again.");
                }
            }

            var duplicateWindowStart = payment.PaymentDate.AddMinutes(-10);
            var duplicateWindowEnd = payment.PaymentDate.AddMinutes(10);
            var recentCandidates = await _context.Payments
                .AsNoTracking()
                .Where(existing =>
                    existing.FolioId == payment.FolioId &&
                    existing.Status != PaymentStatus.Voided &&
                    existing.Status != PaymentStatus.Failed &&
                    existing.Amount == payment.Amount &&
                    existing.PaymentDate >= duplicateWindowStart &&
                    existing.PaymentDate <= duplicateWindowEnd)
                .Select(existing => new { existing.PaymentMethod, existing.ReferenceNumber })
                .ToListAsync();

            if (recentCandidates.Any(existing =>
                    NormalizePaymentMethod(existing.PaymentMethod) == normalizedPaymentMethod &&
                    (string.IsNullOrWhiteSpace(normalizedReference) ||
                        NormalizeReference(existing.ReferenceNumber) == normalizedReference)))
            {
                errors.Add("A similar payment was already posted recently. Review the folio before retrying to avoid duplicate settlement.");
            }
        }

        var shift = await GetOpenShiftForUserAsync(createdBy);
        if (shift is null && !allowWithoutOpenShift)
        {
            errors.Add("Open a cashier shift before posting payments.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            payment.Status = PaymentStatus.Completed;
            payment.PaymentMethod = payment.PaymentMethod.Trim();
            payment.ReferenceNumber = string.IsNullOrWhiteSpace(payment.ReferenceNumber) ? null : payment.ReferenceNumber.Trim();
            payment.Notes = BuildPaymentNotes(payment.Notes, shift, createdBy);
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            if (shift is not null)
            {
                _context.CashierTransactions.Add(new CashierTransaction
                {
                    CashierShiftId = shift.Id,
                    PaymentId = payment.Id,
                    FolioId = payment.FolioId,
                    TransactionDate = payment.PaymentDate,
                    TransactionType = CashierTransactionType.Payment,
                    Amount = payment.Amount,
                    PaymentMethod = MapPaymentMethod(payment.PaymentMethod),
                    ReferenceNumber = payment.ReferenceNumber,
                    Notes = payment.Notes,
                    CreatedBy = createdBy
                });
                await _context.SaveChangesAsync();
            }

            await CloseSettledCheckedOutFolioAsync(payment.FolioId);

            await transaction.CommitAsync();
            return errors;
        });
    }

    private async Task CloseSettledCheckedOutFolioAsync(int folioId)
    {
        var folio = await _context.Folios
            .Include(item => item.Reservation)
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .FirstOrDefaultAsync(item => item.Id == folioId);

        if (folio is null ||
            folio.Status != FolioStatus.Open ||
            folio.Reservation?.Status != Models.FrontOffice.ReservationStatus.CheckedOut ||
            folio.Balance > 0)
        {
            return;
        }

        folio.Status = FolioStatus.Closed;
        folio.ClosedAtUtc ??= DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static string FormatFolioStatus(FolioStatus status)
    {
        return status switch
        {
            FolioStatus.Transferred => "transferred to AR",
            _ => status.ToString().ToLowerInvariant()
        };
    }

    private static string? BuildPaymentNotes(string? notes, CashierShift? shift, string createdBy)
    {
        if (shift is not null)
        {
            return string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        }

        var managementNote = $"Management-posted without open cashier shift by {createdBy}.";
        return string.IsNullOrWhiteSpace(notes)
            ? managementNote
            : $"{notes.Trim()} {managementNote}";
    }

    private static string NormalizePaymentMethod(string? paymentMethod)
    {
        return string.IsNullOrWhiteSpace(paymentMethod)
            ? string.Empty
            : paymentMethod.Replace("-", string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();
    }

    private static string NormalizeReference(string? referenceNumber)
    {
        return string.IsNullOrWhiteSpace(referenceNumber)
            ? string.Empty
            : referenceNumber.Trim().ToUpperInvariant();
    }

    public FinancePaymentMethod MapPaymentMethod(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return FinancePaymentMethod.Other;
        }

        var normalized = paymentMethod.Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        return normalized switch
        {
            "CASH" => FinancePaymentMethod.Cash,
            "CREDITCARD" or "CARD" => FinancePaymentMethod.CreditCard,
            "DEBITCARD" => FinancePaymentMethod.DebitCard,
            "BANKTRANSFER" or "TRANSFER" => FinancePaymentMethod.BankTransfer,
            "EWALLET" or "EWALLETS" or "GCASH" or "MAYA" => FinancePaymentMethod.EWallet,
            "COMPANYCHARGE" or "CITYLEDGER" => FinancePaymentMethod.CompanyCharge,
            "GIFTCERTIFICATE" => FinancePaymentMethod.GiftCertificate,
            _ => FinancePaymentMethod.Other
        };
    }

    public decimal CalculateExpectedCash(CashierShift shift)
    {
        var cashPayments = shift.Transactions
            .Where(transaction =>
                !transaction.IsVoided &&
                transaction.TransactionType == CashierTransactionType.Payment &&
                transaction.PaymentMethod == FinancePaymentMethod.Cash)
            .Sum(transaction => transaction.Amount);

        var cashRefunds = shift.Transactions
            .Where(transaction =>
                !transaction.IsVoided &&
                transaction.TransactionType == CashierTransactionType.Refund &&
                transaction.PaymentMethod == FinancePaymentMethod.Cash)
            .Sum(transaction => transaction.Amount);

        var cashDrops = shift.CashDrops.Sum(drop => drop.Amount);

        return shift.OpeningCashFloat + cashPayments - cashRefunds - cashDrops;
    }

    public void RecalculateFinanceDocument(FinanceDocument document)
    {
        foreach (var line in document.Lines)
        {
            RecalculateFinanceDocumentLine(line);
        }

        document.SubTotal = document.Lines.Sum(line => line.Quantity * line.UnitPrice);
        document.TaxAmount = document.Lines.Sum(line => line.TaxAmount);
        document.ServiceCharge = document.Lines.Sum(line => line.ServiceCharge);
        document.DiscountAmount = document.Lines.Sum(line => line.DiscountAmount);
        document.TotalAmount = document.SubTotal + document.TaxAmount + document.ServiceCharge - document.DiscountAmount;
        document.Balance = document.TotalAmount - document.AmountPaid;
    }

    public void RecalculateFinanceDocumentLine(FinanceDocumentLine line)
    {
        line.LineTotal = (line.Quantity * line.UnitPrice) + line.TaxAmount + line.ServiceCharge - line.DiscountAmount;
    }

    public async Task RecalculateARAccountBalanceAsync(int arAccountId)
    {
        var account = await _context.ARAccounts.FindAsync(arAccountId);
        if (account is null)
        {
            return;
        }

        account.CurrentBalance = await _context.ARInvoices
            .Where(invoice => invoice.ARAccountId == arAccountId &&
                invoice.Status != ARInvoiceStatus.Paid &&
                invoice.Status != ARInvoiceStatus.Cancelled &&
                invoice.Status != ARInvoiceStatus.WrittenOff)
            .SumAsync(invoice => invoice.Balance);

        await _context.SaveChangesAsync();
    }

    public async Task<IList<string>> AllocateARPaymentAsync(int arPaymentId, int arInvoiceId, decimal amount, string allocatedBy)
    {
        var errors = new List<string>();
        var payment = await _context.ARPayments
            .Include(item => item.Allocations)
            .FirstOrDefaultAsync(item => item.Id == arPaymentId);
        var invoice = await _context.ARInvoices.FirstOrDefaultAsync(item => item.Id == arInvoiceId);

        if (payment is null)
        {
            errors.Add("AR payment was not found.");
            return errors;
        }

        if (invoice is null)
        {
            errors.Add("AR invoice was not found.");
            return errors;
        }

        var remainingPayment = payment.Amount - payment.Allocations.Sum(allocation => allocation.AllocatedAmount);
        if (amount <= 0)
        {
            errors.Add("Allocated amount must be greater than zero.");
        }

        if (amount > remainingPayment)
        {
            errors.Add("Allocation cannot exceed remaining payment amount.");
        }

        if (amount > invoice.Balance)
        {
            errors.Add("Allocation cannot exceed invoice balance.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        _context.ARPaymentAllocations.Add(new ARPaymentAllocation
        {
            ARPaymentId = arPaymentId,
            ARInvoiceId = arInvoiceId,
            AllocatedAmount = amount,
            AllocationDate = DateTime.Now,
            AllocatedBy = allocatedBy
        });

        invoice.AmountPaid += amount;
        invoice.Balance = Math.Max(0, invoice.OriginalAmount - invoice.AmountPaid);
        invoice.Status = invoice.Balance == 0
            ? ARInvoiceStatus.Paid
            : ARInvoiceStatus.PartiallyPaid;

        await _context.SaveChangesAsync();
        await RecalculateARAccountBalanceAsync(invoice.ARAccountId);
        return errors;
    }

    public async Task<IList<string>> ApplyCreditMemoAsync(int creditMemoId)
    {
        var errors = new List<string>();
        var memo = await _context.CreditMemos.FirstOrDefaultAsync(item => item.Id == creditMemoId);
        if (memo is null)
        {
            errors.Add("Credit memo was not found.");
            return errors;
        }

        if (memo.Status != MemoStatus.Approved)
        {
            errors.Add("Only approved credit memos can be applied.");
            return errors;
        }

        if (memo.ARInvoiceId is not null)
        {
            var invoice = await _context.ARInvoices.FindAsync(memo.ARInvoiceId);
            if (invoice is null)
            {
                errors.Add("AR invoice was not found.");
                return errors;
            }

            var appliedAmount = Math.Min(memo.Amount, invoice.Balance);
            invoice.Balance -= appliedAmount;
            invoice.AmountPaid += appliedAmount;
            invoice.Status = invoice.Balance == 0 ? ARInvoiceStatus.Paid : ARInvoiceStatus.PartiallyPaid;
            memo.ARAccountId ??= invoice.ARAccountId;
        }

        memo.Status = MemoStatus.Applied;
        memo.AppliedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        if (memo.ARAccountId is not null)
        {
            await RecalculateARAccountBalanceAsync(memo.ARAccountId.Value);
        }

        return errors;
    }

    public async Task<IList<string>> ApplyDebitMemoAsync(int debitMemoId)
    {
        var errors = new List<string>();
        var memo = await _context.DebitMemos.FirstOrDefaultAsync(item => item.Id == debitMemoId);
        if (memo is null)
        {
            errors.Add("Debit memo was not found.");
            return errors;
        }

        if (memo.Status != MemoStatus.Approved)
        {
            errors.Add("Only approved debit memos can be applied.");
            return errors;
        }

        if (memo.ARInvoiceId is not null)
        {
            var invoice = await _context.ARInvoices.FindAsync(memo.ARInvoiceId);
            if (invoice is null)
            {
                errors.Add("AR invoice was not found.");
                return errors;
            }

            invoice.OriginalAmount += memo.Amount;
            invoice.Balance += memo.Amount;
            if (invoice.Status == ARInvoiceStatus.Paid)
            {
                invoice.Status = ARInvoiceStatus.PartiallyPaid;
            }

            memo.ARAccountId ??= invoice.ARAccountId;
        }

        memo.Status = MemoStatus.Applied;
        memo.AppliedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        if (memo.ARAccountId is not null)
        {
            await RecalculateARAccountBalanceAsync(memo.ARAccountId.Value);
        }

        return errors;
    }

    public async Task<IList<string>> ProcessVoidRequestAsync(int id, string processedBy)
    {
        var errors = new List<string>();
        var request = await _context.VoidRequests.FindAsync(id);
        if (request is null)
        {
            errors.Add("Void request was not found.");
            return errors;
        }

        if (request.Status != ApprovalStatus.Approved)
        {
            errors.Add("Only approved void requests can be processed.");
            return errors;
        }

        switch (request.ReferenceType)
        {
            case "FolioItem":
                var item = await _context.FolioItems.FindAsync(request.ReferenceId);
                if (item is null) errors.Add("Folio item was not found."); else item.IsVoided = true;
                break;
            case "Payment":
                var payment = await _context.Payments.FindAsync(request.ReferenceId);
                if (payment is null) errors.Add("Payment was not found."); else payment.Status = PaymentStatus.Voided;
                break;
            case "POSOrder":
                var posOrder = await _context.POSOrders.FindAsync(request.ReferenceId);
                if (posOrder is null) errors.Add("POS order was not found."); else posOrder.PaymentStatus = POSPaymentStatus.Voided;
                break;
            case "FinanceDocument":
                var document = await _context.FinanceDocuments.FindAsync(request.ReferenceId);
                if (document is null)
                {
                    errors.Add("Finance document was not found.");
                }
                else
                {
                    document.Status = FinanceDocumentStatus.Voided;
                    document.VoidedBy = processedBy;
                    document.VoidedAt = DateTime.Now;
                    document.VoidReason = request.Reason;
                }
                break;
            default:
                errors.Add("Unsupported void reference type.");
                break;
        }

        if (errors.Count == 0)
        {
            request.Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? $"Processed by {processedBy}."
                : $"{request.Notes} Processed by {processedBy}.";
            await _context.SaveChangesAsync();
        }

        return errors;
    }
}
