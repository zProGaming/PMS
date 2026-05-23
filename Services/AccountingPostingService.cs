using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Services;

public class AccountingPostingService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<JournalEntry?> CreateJournalEntryFromFolioItemAsync(int folioItemId, string createdBy)
    {
        var item = await _context.FolioItems
            .AsNoTracking()
            .Include(folioItem => folioItem.Folio)
            .Include(folioItem => folioItem.ChargeCodeDefinition)
            .FirstOrDefaultAsync(folioItem => folioItem.Id == folioItemId && !folioItem.IsVoided);

        if (item is null)
        {
            return null;
        }

        if (IsChargeToRoomDisplayFolioItem(item.ChargeCode, item.Description))
        {
            return null;
        }

        var transactionType = item.ChargeCode.Equals("ROOM", StringComparison.OrdinalIgnoreCase)
            ? SourceTransactionType.RoomCharge
            : SourceTransactionType.FolioCharge;
        var sourceModule = transactionType == SourceTransactionType.RoomCharge
            ? SourceModule.FrontOffice
            : SourceModule.Finance;

        return await CreateJournalFromRuleAsync(
            sourceModule,
            transactionType,
            item.Id,
            item.Folio?.FolioNumber ?? item.Id.ToString(),
            item.Amount,
            item.Description,
            item.PostingDate,
            createdBy,
            item.ChargeCodeId,
            null,
            null,
            PostingAmounts.FromFolioItem(item));
    }

    public async Task<JournalEntry?> CreateJournalEntryFromPaymentAsync(int paymentId, string createdBy)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .Include(item => item.Folio)
            .FirstOrDefaultAsync(item => item.Id == paymentId && item.Status == PaymentStatus.Completed);

        if (payment is null)
        {
            return null;
        }

        return await CreateJournalFromRuleAsync(
            SourceModule.Finance,
            SourceTransactionType.FolioPayment,
            payment.Id,
            payment.ReferenceNumber ?? payment.Id.ToString(),
            payment.Amount,
            $"Folio payment - {payment.Folio?.FolioNumber ?? payment.FolioId.ToString()}",
            payment.PaymentDate,
            createdBy,
            null,
            NormalizePaymentMethod(payment.PaymentMethod),
            null);
    }

    public async Task<JournalEntry?> CreateJournalEntryFromPOSOrderAsync(int posOrderId, string createdBy)
    {
        var order = await _context.POSOrders
            .AsNoTracking()
            .Include(item => item.Outlet)
            .FirstOrDefaultAsync(item =>
                item.Id == posOrderId &&
                item.OrderStatus == POSOrderStatus.Closed &&
                item.PaymentStatus != POSPaymentStatus.Unpaid &&
                item.PaymentStatus != POSPaymentStatus.Voided);

        if (order is null)
        {
            return null;
        }

        var transactionType = order.PaymentStatus == POSPaymentStatus.ChargedToRoom
            ? SourceTransactionType.POSChargeToRoom
            : SourceTransactionType.POSPayment;

        var fbChargeCodeId = await _context.ChargeCodes
            .AsNoTracking()
            .Where(chargeCode => chargeCode.Code == "FB" && chargeCode.IsActive)
            .Select(chargeCode => (int?)chargeCode.Id)
            .FirstOrDefaultAsync();

        var paymentMethod = order.PaymentStatus == POSPaymentStatus.ChargedToRoom
            ? null
            : ResolvePOSPaymentMethod(order.Notes);

        return await CreateJournalFromRuleAsync(
            SourceModule.FoodBeverage,
            transactionType,
            order.Id,
            order.OrderNumber,
            order.TotalAmount,
            $"F&B order - {order.Outlet?.Name ?? "Outlet"} - Order #{order.OrderNumber}",
            order.ClosedAt ?? order.OrderDate,
            createdBy,
            fbChargeCodeId,
            paymentMethod,
            null,
            PostingAmounts.FromPOSOrder(order));
    }

    public async Task<JournalEntry?> CreateJournalEntryFromBanquetChargeAsync(int banquetChargeId, string createdBy)
    {
        var charge = await _context.BanquetCharges
            .AsNoTracking()
            .Include(item => item.BanquetEvent)
            .FirstOrDefaultAsync(item => item.Id == banquetChargeId && !item.IsVoided);

        if (charge is null)
        {
            return null;
        }

        return await CreateJournalFromRuleAsync(
            SourceModule.Banquet,
            SourceTransactionType.BanquetCharge,
            charge.Id,
            charge.BanquetEvent?.EventName ?? charge.Id.ToString(),
            charge.Amount,
            charge.Description,
            charge.ChargeDate,
            createdBy,
            null,
            null,
            null);
    }

    public async Task<JournalEntry?> CreateJournalEntryFromARInvoiceAsync(int arInvoiceId, string createdBy)
    {
        var invoice = await _context.ARInvoices
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == arInvoiceId && item.Status != ARInvoiceStatus.Cancelled);

        if (invoice is null)
        {
            return null;
        }

        return await CreateJournalFromRuleAsync(
            SourceModule.AccountsReceivable,
            SourceTransactionType.ARInvoice,
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.OriginalAmount,
            $"AR invoice - {invoice.InvoiceNumber}",
            invoice.InvoiceDate,
            createdBy,
            null,
            null,
            null);
    }

    public async Task<JournalEntry?> CreateJournalEntryFromARPaymentAsync(int arPaymentId, string createdBy)
    {
        var payment = await _context.ARPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == arPaymentId);

        if (payment is null)
        {
            return null;
        }

        return await CreateJournalFromRuleAsync(
            SourceModule.AccountsReceivable,
            SourceTransactionType.ARPayment,
            payment.Id,
            payment.ReferenceNumber ?? payment.Id.ToString(),
            payment.Amount,
            $"AR payment - {payment.ReferenceNumber ?? payment.Id.ToString()}",
            payment.PaymentDate,
            createdBy,
            null,
            NormalizePaymentMethod(payment.PaymentMethod.ToString()),
            null);
    }

    public async Task<JournalEntry?> CreateJournalEntryFromPurchaseReceivingAsync(int receivingRecordId, string createdBy)
    {
        var receiving = await _context.ReceivingRecords
            .AsNoTracking()
            .Include(record => record.Items)
            .FirstOrDefaultAsync(record => record.Id == receivingRecordId && record.Status == ReceivingStatus.Posted);

        if (receiving is null)
        {
            return null;
        }

        return await CreateJournalFromRuleAsync(
            SourceModule.Purchasing,
            SourceTransactionType.PurchaseReceiving,
            receiving.Id,
            receiving.ReceivingNumber,
            receiving.Items.Sum(item => item.Amount),
            $"Purchase receiving - {receiving.ReceivingNumber}",
            receiving.ReceivedDate,
            createdBy,
            null,
            null,
            null);
    }

    public async Task<JournalEntry?> CreateJournalEntryFromStockIssueAsync(int stockMovementId, string createdBy)
    {
        var movement = await _context.StockMovements
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == stockMovementId && item.MovementType == StockMovementType.StockIssue);

        if (movement is null)
        {
            return null;
        }

        return await CreateJournalFromRuleAsync(
            SourceModule.Inventory,
            SourceTransactionType.StockIssue,
            movement.Id,
            movement.ReferenceId?.ToString() ?? movement.Id.ToString(),
            movement.Quantity * movement.UnitCost,
            movement.Remarks ?? "Stock issue",
            movement.MovementDate,
            createdBy,
            null,
            null,
            null);
    }

    public async Task<JournalEntry?> CreateJournalEntryFromStockAdjustmentAsync(int stockMovementId, string createdBy)
    {
        var movement = await _context.StockMovements
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == stockMovementId &&
                (item.MovementType == StockMovementType.AdjustmentIncrease ||
                    item.MovementType == StockMovementType.AdjustmentDecrease));

        if (movement is null)
        {
            return null;
        }

        var transactionType = movement.MovementType == StockMovementType.AdjustmentIncrease
            ? SourceTransactionType.StockAdjustmentIncrease
            : SourceTransactionType.StockAdjustmentDecrease;

        return await CreateJournalFromRuleAsync(
            SourceModule.Inventory,
            transactionType,
            movement.Id,
            movement.ReferenceId?.ToString() ?? movement.Id.ToString(),
            Math.Abs(movement.Quantity * movement.UnitCost),
            movement.Remarks ?? "Stock adjustment",
            movement.MovementDate,
            createdBy,
            null,
            null,
            null);
    }

    public async Task<JournalEntry> CreateManualJournalEntryAsync(JournalEntry journalEntry)
    {
        journalEntry.SourceModule = SourceModule.Manual;
        journalEntry.SourceTransactionType = SourceTransactionType.ManualJournal;
        journalEntry.Status = JournalEntryStatus.Draft;
        journalEntry.JournalNumber = string.IsNullOrWhiteSpace(journalEntry.JournalNumber)
            ? await GenerateJournalNumberAsync()
            : journalEntry.JournalNumber;
        journalEntry.AccountingPeriodId ??= await FindOpenPeriodIdAsync(journalEntry.JournalDate);
        _context.JournalEntries.Add(journalEntry);
        await _context.SaveChangesAsync();
        return journalEntry;
    }

    public bool ValidateJournalEntryBalance(JournalEntry journalEntry)
    {
        if (journalEntry.Lines.Count < 2)
        {
            return false;
        }

        if (journalEntry.Lines.Any(line =>
                line.GLAccountId <= 0 ||
                line.DebitAmount < 0 ||
                line.CreditAmount < 0 ||
                (line.DebitAmount == 0 && line.CreditAmount == 0) ||
                (line.DebitAmount > 0 && line.CreditAmount > 0)))
        {
            return false;
        }

        return Math.Round(journalEntry.Lines.Sum(line => line.DebitAmount), 2, MidpointRounding.AwayFromZero) ==
            Math.Round(journalEntry.Lines.Sum(line => line.CreditAmount), 2, MidpointRounding.AwayFromZero);
    }

    public async Task<IList<string>> PostJournalEntryAsync(int journalEntryId, string postedBy)
    {
        var errors = new List<string>();
        var entry = await _context.JournalEntries
            .Include(item => item.Lines)
            .ThenInclude(line => line.GLAccount)
            .FirstOrDefaultAsync(item => item.Id == journalEntryId);

        if (entry is null)
        {
            errors.Add("Journal entry was not found.");
            return errors;
        }

        if (entry.Status != JournalEntryStatus.Draft)
        {
            errors.Add("Only draft journal entries can be posted.");
        }

        if (!ValidateJournalEntryBalance(entry))
        {
            errors.Add("Journal entry must have at least two valid lines and total debits must equal total credits.");
        }

        if (entry.Lines.Any(line => line.GLAccount is null || !line.GLAccount.IsActive))
        {
            errors.Add("Journal entries cannot be posted to inactive or missing GL accounts.");
        }

        var openPeriodId = await FindOpenPeriodIdAsync(entry.JournalDate);
        if (await _context.AccountingPeriods.AnyAsync() && openPeriodId is null)
        {
            errors.Add("Journal date must fall within an open accounting period.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        entry.AccountingPeriodId ??= openPeriodId;
        entry.Status = JournalEntryStatus.Posted;
        entry.PostedBy = postedBy;
        entry.PostedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return errors;
    }

    public async Task<IList<string>> ReverseJournalEntryAsync(int journalEntryId, string reversedBy, string reversalReason)
    {
        var errors = new List<string>();
        var entry = await _context.JournalEntries
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == journalEntryId);

        if (entry is null)
        {
            errors.Add("Journal entry was not found.");
            return errors;
        }

        if (entry.Status != JournalEntryStatus.Posted)
        {
            errors.Add("Only posted journal entries can be reversed.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(reversalReason))
        {
            errors.Add("Reversal reason is required.");
            return errors;
        }

        var reversal = new JournalEntry
        {
            JournalNumber = await GenerateJournalNumberAsync("REV"),
            JournalDate = DateTime.Today,
            AccountingPeriodId = await FindOpenPeriodIdAsync(DateTime.Today),
            SourceModule = entry.SourceModule,
            SourceTransactionType = entry.SourceTransactionType,
            SourceReferenceId = entry.SourceReferenceId,
            SourceReferenceNumber = entry.SourceReferenceNumber,
            Description = $"Reversal of {entry.JournalNumber}: {reversalReason}",
            Status = JournalEntryStatus.Posted,
            CreatedBy = reversedBy,
            PostedBy = reversedBy,
            PostedAt = DateTime.Now,
            Lines = entry.Lines.Select(line => new JournalEntryLine
            {
                GLAccountId = line.GLAccountId,
                USALIDepartmentId = line.USALIDepartmentId,
                DebitAmount = line.CreditAmount,
                CreditAmount = line.DebitAmount,
                Description = $"Reversal - {line.Description}",
                LineReferenceType = line.LineReferenceType,
                LineReferenceId = line.LineReferenceId
            }).ToList()
        };

        _context.JournalEntries.Add(reversal);
        entry.Status = JournalEntryStatus.Reversed;
        entry.ReversedBy = reversedBy;
        entry.ReversedAt = DateTime.Now;
        entry.ReversalReason = reversalReason;
        await _context.SaveChangesAsync();
        return errors;
    }

    public async Task<PostingBatch> CreatePostingBatchAsync(SourceModule sourceModule, DateTime startDate, DateTime endDate, string createdBy)
    {
        var batch = new PostingBatch
        {
            BatchNumber = await GenerateBatchNumberAsync(),
            BatchDate = DateTime.Today,
            SourceModule = sourceModule,
            Status = PostingBatchStatus.Draft,
            CreatedBy = createdBy,
            Notes = $"Source date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}."
        };

        _context.PostingBatches.Add(batch);
        await _context.SaveChangesAsync();

        var items = await FindEligibleBatchItemsAsync(sourceModule, startDate.Date, endDate.Date);
        foreach (var item in items)
        {
            item.PostingBatchId = batch.Id;
            _context.PostingBatchItems.Add(item);
        }

        await _context.SaveChangesAsync();
        return batch;
    }

    public async Task<IList<string>> ProcessPostingBatchAsync(int postingBatchId, string postedBy)
    {
        var errors = new List<string>();
        var batch = await _context.PostingBatches
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == postingBatchId);

        if (batch is null)
        {
            errors.Add("Posting batch was not found.");
            return errors;
        }

        if (batch.Status is PostingBatchStatus.Posted or PostingBatchStatus.Cancelled)
        {
            errors.Add("This posting batch cannot be processed.");
            return errors;
        }

        batch.Status = PostingBatchStatus.Processing;
        await _context.SaveChangesAsync();

        foreach (var item in batch.Items.Where(item => item.Status == PostingBatchItemStatus.Pending))
        {
            try
            {
                var entry = await CreateJournalEntryForBatchItemAsync(batch.SourceModule, item, postedBy);
                if (entry is null)
                {
                    item.Status = PostingBatchItemStatus.Skipped;
                    item.ErrorMessage = "No eligible source transaction or posting rule was found.";
                    continue;
                }

                var postErrors = await PostJournalEntryAsync(entry.Id, postedBy);
                if (postErrors.Count > 0)
                {
                    item.Status = PostingBatchItemStatus.Error;
                    item.ErrorMessage = string.Join(" ", postErrors);
                    continue;
                }

                item.JournalEntryId = entry.Id;
                item.Status = PostingBatchItemStatus.Posted;
            }
            catch (Exception ex)
            {
                item.Status = PostingBatchItemStatus.Error;
                item.ErrorMessage = ex.Message;
            }
        }

        batch.PostedBy = postedBy;
        batch.PostedAt = DateTime.Now;
        batch.Status = batch.Items.Any(item => item.Status == PostingBatchItemStatus.Error)
            ? PostingBatchStatus.PostedWithErrors
            : PostingBatchStatus.Posted;
        await _context.SaveChangesAsync();
        return errors;
    }

    private async Task<JournalEntry?> CreateJournalEntryForBatchItemAsync(SourceModule sourceModule, PostingBatchItem item, string postedBy)
    {
        return item.SourceTransactionType switch
        {
            SourceTransactionType.FolioCharge or SourceTransactionType.RoomCharge => await CreateJournalEntryFromFolioItemAsync(item.SourceReferenceId, postedBy),
            SourceTransactionType.FolioPayment => await CreateJournalEntryFromPaymentAsync(item.SourceReferenceId, postedBy),
            SourceTransactionType.POSPayment or SourceTransactionType.POSChargeToRoom => await CreateJournalEntryFromPOSOrderAsync(item.SourceReferenceId, postedBy),
            SourceTransactionType.BanquetCharge => await CreateJournalEntryFromBanquetChargeAsync(item.SourceReferenceId, postedBy),
            SourceTransactionType.ARInvoice => await CreateJournalEntryFromARInvoiceAsync(item.SourceReferenceId, postedBy),
            SourceTransactionType.ARPayment => await CreateJournalEntryFromARPaymentAsync(item.SourceReferenceId, postedBy),
            SourceTransactionType.PurchaseReceiving => await CreateJournalEntryFromPurchaseReceivingAsync(item.SourceReferenceId, postedBy),
            SourceTransactionType.StockIssue => await CreateJournalEntryFromStockIssueAsync(item.SourceReferenceId, postedBy),
            SourceTransactionType.StockAdjustmentIncrease or SourceTransactionType.StockAdjustmentDecrease => await CreateJournalEntryFromStockAdjustmentAsync(item.SourceReferenceId, postedBy),
            _ => null
        };
    }

    private async Task<List<PostingBatchItem>> FindEligibleBatchItemsAsync(SourceModule sourceModule, DateTime startDate, DateTime endDate)
    {
        var endExclusive = endDate.AddDays(1);
        var items = new List<PostingBatchItem>();

        if (sourceModule is SourceModule.Finance or SourceModule.FrontOffice)
        {
            var folioItems = await _context.FolioItems
                .AsNoTracking()
                .Where(item => !item.IsVoided && item.PostingDate >= startDate && item.PostingDate < endExclusive)
                .Select(item => new { item.Id, item.ChargeCode, item.Description, Reference = item.Id.ToString() })
                .ToListAsync();
            foreach (var item in folioItems)
            {
                if (IsChargeToRoomDisplayFolioItem(item.ChargeCode, item.Description))
                {
                    continue;
                }

                var transactionType = item.ChargeCode.Equals("ROOM", StringComparison.OrdinalIgnoreCase)
                    ? SourceTransactionType.RoomCharge
                    : SourceTransactionType.FolioCharge;
                var postedSource = transactionType == SourceTransactionType.RoomCharge ? SourceModule.FrontOffice : SourceModule.Finance;
                if (!await HasPostedJournalAsync(postedSource, transactionType, item.Id))
                {
                    items.Add(BatchItem(transactionType, item.Id, item.Reference));
                }
            }

            var payments = await _context.Payments
                .AsNoTracking()
                .Where(payment => payment.Status == PaymentStatus.Completed && payment.PaymentDate >= startDate && payment.PaymentDate < endExclusive)
                .Select(payment => new { payment.Id, payment.ReferenceNumber })
                .ToListAsync();
            foreach (var payment in payments)
            {
                if (!await HasPostedJournalAsync(SourceModule.Finance, SourceTransactionType.FolioPayment, payment.Id))
                {
                    items.Add(BatchItem(SourceTransactionType.FolioPayment, payment.Id, payment.ReferenceNumber ?? payment.Id.ToString()));
                }
            }
        }

        if (sourceModule == SourceModule.FoodBeverage)
        {
            var orders = await _context.POSOrders
                .AsNoTracking()
                .Where(order => order.OrderStatus == POSOrderStatus.Closed && order.ClosedAt >= startDate && order.ClosedAt < endExclusive)
                .Select(order => new { order.Id, order.OrderNumber, order.PaymentStatus })
                .ToListAsync();
            foreach (var order in orders)
            {
                var transactionType = order.PaymentStatus == POSPaymentStatus.ChargedToRoom ? SourceTransactionType.POSChargeToRoom : SourceTransactionType.POSPayment;
                if (!await HasPostedJournalAsync(SourceModule.FoodBeverage, transactionType, order.Id))
                {
                    items.Add(BatchItem(transactionType, order.Id, order.OrderNumber));
                }
            }
        }

        if (sourceModule == SourceModule.Banquet)
        {
            var charges = await _context.BanquetCharges
                .AsNoTracking()
                .Where(charge => !charge.IsVoided && charge.ChargeDate >= startDate && charge.ChargeDate < endExclusive)
                .Select(charge => new { charge.Id, charge.Description })
                .ToListAsync();
            foreach (var charge in charges)
            {
                if (!await HasPostedJournalAsync(SourceModule.Banquet, SourceTransactionType.BanquetCharge, charge.Id))
                {
                    items.Add(BatchItem(SourceTransactionType.BanquetCharge, charge.Id, charge.Description));
                }
            }
        }

        if (sourceModule == SourceModule.AccountsReceivable)
        {
            var invoices = await _context.ARInvoices
                .AsNoTracking()
                .Where(invoice => invoice.InvoiceDate >= startDate && invoice.InvoiceDate < endExclusive && invoice.Status != ARInvoiceStatus.Cancelled)
                .Select(invoice => new { invoice.Id, invoice.InvoiceNumber })
                .ToListAsync();
            foreach (var invoice in invoices)
            {
                if (!await HasPostedJournalAsync(SourceModule.AccountsReceivable, SourceTransactionType.ARInvoice, invoice.Id))
                {
                    items.Add(BatchItem(SourceTransactionType.ARInvoice, invoice.Id, invoice.InvoiceNumber));
                }
            }

            var payments = await _context.ARPayments
                .AsNoTracking()
                .Where(payment => payment.PaymentDate >= startDate && payment.PaymentDate < endExclusive)
                .Select(payment => new { payment.Id, payment.ReferenceNumber })
                .ToListAsync();
            foreach (var payment in payments)
            {
                if (!await HasPostedJournalAsync(SourceModule.AccountsReceivable, SourceTransactionType.ARPayment, payment.Id))
                {
                    items.Add(BatchItem(SourceTransactionType.ARPayment, payment.Id, payment.ReferenceNumber ?? payment.Id.ToString()));
                }
            }
        }

        if (sourceModule == SourceModule.Purchasing)
        {
            var receivingRecords = await _context.ReceivingRecords
                .AsNoTracking()
                .Where(record => record.Status == ReceivingStatus.Posted && record.ReceivedDate >= startDate && record.ReceivedDate < endExclusive)
                .Select(record => new { record.Id, record.ReceivingNumber })
                .ToListAsync();
            foreach (var record in receivingRecords)
            {
                if (!await HasPostedJournalAsync(SourceModule.Purchasing, SourceTransactionType.PurchaseReceiving, record.Id))
                {
                    items.Add(BatchItem(SourceTransactionType.PurchaseReceiving, record.Id, record.ReceivingNumber));
                }
            }
        }

        if (sourceModule == SourceModule.Inventory)
        {
            var movements = await _context.StockMovements
                .AsNoTracking()
                .Where(movement => movement.MovementDate >= startDate && movement.MovementDate < endExclusive)
                .Select(movement => new { movement.Id, movement.MovementType, Reference = movement.ReferenceId })
                .ToListAsync();
            foreach (var movement in movements)
            {
                var transactionType = movement.MovementType switch
                {
                    StockMovementType.StockIssue => SourceTransactionType.StockIssue,
                    StockMovementType.AdjustmentIncrease => SourceTransactionType.StockAdjustmentIncrease,
                    StockMovementType.AdjustmentDecrease => SourceTransactionType.StockAdjustmentDecrease,
                    _ => (SourceTransactionType?)null
                };

                if (transactionType is not null && !await HasPostedJournalAsync(SourceModule.Inventory, transactionType.Value, movement.Id))
                {
                    items.Add(BatchItem(transactionType.Value, movement.Id, movement.Reference?.ToString() ?? movement.Id.ToString()));
                }
            }
        }

        return items;
    }

    private static PostingBatchItem BatchItem(SourceTransactionType transactionType, int referenceId, string? referenceNumber)
    {
        return new PostingBatchItem
        {
            SourceTransactionType = transactionType,
            SourceReferenceId = referenceId,
            SourceReferenceNumber = referenceNumber,
            Status = PostingBatchItemStatus.Pending
        };
    }

    private async Task<JournalEntry?> CreateJournalFromRuleAsync(
        SourceModule sourceModule,
        SourceTransactionType transactionType,
        int sourceReferenceId,
        string? sourceReferenceNumber,
        decimal amount,
        string description,
        DateTime journalDate,
        string createdBy,
        int? chargeCodeId,
        string? paymentMethod,
        int? usaliDepartmentId,
        PostingAmounts? postingAmounts = null)
    {
        var amounts = postingAmounts ?? PostingAmounts.FromAmount(amount);
        if (amounts.IsZero || await HasPostedJournalAsync(sourceModule, transactionType, sourceReferenceId))
        {
            return null;
        }

        var normalizedPaymentMethod = NormalizePaymentMethod(paymentMethod);
        var rule = await FindPostingRuleAsync(sourceModule, transactionType, chargeCodeId, normalizedPaymentMethod);
        if (rule is null)
        {
            return null;
        }

        var journalEntry = new JournalEntry
        {
            JournalNumber = await GenerateJournalNumberAsync(),
            JournalDate = journalDate.Date,
            AccountingPeriodId = await FindOpenPeriodIdAsync(journalDate.Date),
            SourceModule = sourceModule,
            SourceTransactionType = transactionType,
            SourceReferenceId = sourceReferenceId,
            SourceReferenceNumber = sourceReferenceNumber,
            Description = description,
            Status = JournalEntryStatus.Draft,
            CreatedBy = createdBy
        };

        foreach (var line in BuildPostingLines(rule, amounts, transactionType, sourceReferenceId, description, usaliDepartmentId))
        {
            journalEntry.Lines.Add(line);
        }

        _context.JournalEntries.Add(journalEntry);
        await _context.SaveChangesAsync();
        return journalEntry;
    }

    private async Task<PostingRule?> FindPostingRuleAsync(SourceModule sourceModule, SourceTransactionType transactionType, int? chargeCodeId, string? paymentMethod)
    {
        var rules = await _context.PostingRules
            .AsNoTracking()
            .Where(rule => rule.IsActive && rule.SourceModule == sourceModule && rule.TransactionType == transactionType)
            .OrderByDescending(rule => rule.ChargeCodeId == chargeCodeId)
            .ThenByDescending(rule => rule.PaymentMethod == paymentMethod)
            .ToListAsync();

        return rules.FirstOrDefault(rule =>
            (rule.ChargeCodeId == null || rule.ChargeCodeId == chargeCodeId) &&
            (string.IsNullOrWhiteSpace(rule.PaymentMethod) || rule.PaymentMethod == paymentMethod));
    }

    private async Task<bool> HasPostedJournalAsync(SourceModule sourceModule, SourceTransactionType transactionType, int sourceReferenceId)
    {
        return await _context.JournalEntries.AnyAsync(entry =>
            entry.SourceModule == sourceModule &&
            entry.SourceTransactionType == transactionType &&
            entry.SourceReferenceId == sourceReferenceId &&
            entry.Status == JournalEntryStatus.Posted);
    }

    private static List<JournalEntryLine> BuildPostingLines(
        PostingRule rule,
        PostingAmounts amounts,
        SourceTransactionType transactionType,
        int sourceReferenceId,
        string description,
        int? usaliDepartmentId)
    {
        var departmentId = usaliDepartmentId ?? rule.USALIDepartmentId;
        var lines = new List<JournalEntryLine>();

        AddSignedControlLine(lines, rule.DebitGLAccountId, amounts.ControlAmount, departmentId, description, transactionType, sourceReferenceId);

        var revenueCredit = amounts.RevenueAmount;
        if (amounts.TaxAmount > 0)
        {
            if (rule.TaxGLAccountId is not null)
            {
                AddCreditLine(lines, rule.TaxGLAccountId.Value, amounts.TaxAmount, null, $"{description} - output tax", transactionType, sourceReferenceId);
            }
            else
            {
                revenueCredit += amounts.TaxAmount;
            }
        }

        if (amounts.ServiceChargeAmount > 0)
        {
            if (rule.ServiceChargeGLAccountId is not null)
            {
                AddCreditLine(lines, rule.ServiceChargeGLAccountId.Value, amounts.ServiceChargeAmount, null, $"{description} - service charge", transactionType, sourceReferenceId);
            }
            else
            {
                revenueCredit += amounts.ServiceChargeAmount;
            }
        }

        if (amounts.DiscountAmount > 0)
        {
            if (rule.DiscountGLAccountId is not null)
            {
                AddDebitLine(lines, rule.DiscountGLAccountId.Value, amounts.DiscountAmount, departmentId, $"{description} - discount", transactionType, sourceReferenceId);
            }
            else
            {
                revenueCredit -= amounts.DiscountAmount;
            }
        }

        AddSignedRevenueLine(lines, rule.CreditGLAccountId, revenueCredit, departmentId, description, transactionType, sourceReferenceId);
        return lines;
    }

    private static void AddSignedControlLine(
        ICollection<JournalEntryLine> lines,
        int glAccountId,
        decimal amount,
        int? usaliDepartmentId,
        string description,
        SourceTransactionType transactionType,
        int sourceReferenceId)
    {
        amount = Round(amount);
        if (amount > 0)
        {
            AddDebitLine(lines, glAccountId, amount, usaliDepartmentId, description, transactionType, sourceReferenceId);
        }
        else if (amount < 0)
        {
            AddCreditLine(lines, glAccountId, Math.Abs(amount), usaliDepartmentId, description, transactionType, sourceReferenceId);
        }
    }

    private static void AddSignedRevenueLine(
        ICollection<JournalEntryLine> lines,
        int glAccountId,
        decimal amount,
        int? usaliDepartmentId,
        string description,
        SourceTransactionType transactionType,
        int sourceReferenceId)
    {
        amount = Round(amount);
        if (amount > 0)
        {
            AddCreditLine(lines, glAccountId, amount, usaliDepartmentId, description, transactionType, sourceReferenceId);
        }
        else if (amount < 0)
        {
            AddDebitLine(lines, glAccountId, Math.Abs(amount), usaliDepartmentId, $"{description} - reduction", transactionType, sourceReferenceId);
        }
    }

    private static void AddDebitLine(
        ICollection<JournalEntryLine> lines,
        int glAccountId,
        decimal amount,
        int? usaliDepartmentId,
        string description,
        SourceTransactionType transactionType,
        int sourceReferenceId)
    {
        amount = Round(amount);
        if (amount <= 0)
        {
            return;
        }

        lines.Add(new JournalEntryLine
        {
            GLAccountId = glAccountId,
            USALIDepartmentId = usaliDepartmentId,
            DebitAmount = amount,
            CreditAmount = 0,
            Description = description,
            LineReferenceType = transactionType.ToString(),
            LineReferenceId = sourceReferenceId
        });
    }

    private static void AddCreditLine(
        ICollection<JournalEntryLine> lines,
        int glAccountId,
        decimal amount,
        int? usaliDepartmentId,
        string description,
        SourceTransactionType transactionType,
        int sourceReferenceId)
    {
        amount = Round(amount);
        if (amount <= 0)
        {
            return;
        }

        lines.Add(new JournalEntryLine
        {
            GLAccountId = glAccountId,
            USALIDepartmentId = usaliDepartmentId,
            DebitAmount = 0,
            CreditAmount = amount,
            Description = description,
            LineReferenceType = transactionType.ToString(),
            LineReferenceId = sourceReferenceId
        });
    }

    private static bool IsChargeToRoomDisplayFolioItem(string? chargeCode, string? description)
    {
        return string.Equals(chargeCode, "FB", StringComparison.OrdinalIgnoreCase) &&
            description?.Contains("Order #", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static decimal Round(decimal amount)
    {
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private async Task<int?> FindOpenPeriodIdAsync(DateTime journalDate)
    {
        return await _context.AccountingPeriods
            .Where(period =>
                period.Status == AccountingPeriodStatus.Open &&
                period.StartDate <= journalDate.Date &&
                period.EndDate >= journalDate.Date)
            .Select(period => (int?)period.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<string> GenerateJournalNumberAsync(string prefix = "JE")
    {
        var todayPrefix = $"{prefix}-{DateTime.Today:yyyyMMdd}";
        var existingCount = await _context.JournalEntries.CountAsync(entry => entry.JournalNumber.StartsWith(todayPrefix));
        return $"{todayPrefix}-{existingCount + 1:0000}";
    }

    private async Task<string> GenerateBatchNumberAsync()
    {
        var todayPrefix = $"PB-{DateTime.Today:yyyyMMdd}";
        var existingCount = await _context.PostingBatches.CountAsync(batch => batch.BatchNumber.StartsWith(todayPrefix));
        return $"{todayPrefix}-{existingCount + 1:0000}";
    }

    private static string? NormalizePaymentMethod(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return null;
        }

        var normalized = paymentMethod.Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        return normalized switch
        {
            "CASH" => "Cash",
            "CARD" or "CREDITCARD" or "DEBITCARD" => "Card",
            "BANK" or "BANKTRANSFER" or "TRANSFER" => "BankTransfer",
            "EWALLET" or "GCASH" or "MAYA" => "EWallet",
            "COMPANYCHARGE" or "CITYLEDGER" => "CompanyCharge",
            _ => paymentMethod.Trim()
        };
    }

    private static string ResolvePOSPaymentMethod(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return "Cash";
        }

        var settlementLine = notes
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault(line => line.StartsWith("Settlement:", StringComparison.OrdinalIgnoreCase));

        if (settlementLine is null)
        {
            return "Cash";
        }

        var rawMethod = settlementLine["Settlement:".Length..].Trim();
        return NormalizePaymentMethod(rawMethod) ?? "Cash";
    }

    private sealed record PostingAmounts(
        decimal ControlAmount,
        decimal RevenueAmount,
        decimal TaxAmount,
        decimal ServiceChargeAmount,
        decimal DiscountAmount)
    {
        public bool IsZero =>
            ControlAmount == 0 &&
            RevenueAmount == 0 &&
            TaxAmount == 0 &&
            ServiceChargeAmount == 0 &&
            DiscountAmount == 0;

        public static PostingAmounts FromAmount(decimal amount)
        {
            amount = Round(amount);
            return amount >= 0
                ? new PostingAmounts(amount, amount, 0, 0, 0)
                : new PostingAmounts(amount, 0, 0, 0, Math.Abs(amount));
        }

        public static PostingAmounts FromFolioItem(FolioItem item)
        {
            if (item.Amount < 0 ||
                item.ChargeCodeDefinition?.ChargeCategory == ChargeCategory.Discount ||
                string.Equals(item.ChargeCode, "DISC", StringComparison.OrdinalIgnoreCase))
            {
                return new PostingAmounts(Round(item.Amount), 0, 0, 0, Math.Abs(Round(item.Amount)));
            }

            return FromAmount(item.Amount);
        }

        public static PostingAmounts FromPOSOrder(POSOrder order)
        {
            return new PostingAmounts(
                Round(order.TotalAmount),
                Round(order.SubTotal),
                Round(order.TaxAmount),
                Round(order.ServiceCharge),
                Round(order.DiscountAmount));
        }
    }
}
