using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Services;

public class ARCollectionReportService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ARAgingReportResult> GetAgingReportAsync(
        DateTime asOfDate,
        int? arAccountId = null,
        string? status = null,
        string? bucket = null,
        string? search = null,
        bool includePaid = false)
    {
        asOfDate = asOfDate.Date;
        var query = _context.ARInvoices
            .AsNoTracking()
            .Include(invoice => invoice.ARAccount)
            .Include(invoice => invoice.Allocations)
                .ThenInclude(allocation => allocation.ARPayment)
            .Where(invoice =>
                invoice.InvoiceDate.Date <= asOfDate &&
                invoice.Status != ARInvoiceStatus.Cancelled &&
                invoice.Status != ARInvoiceStatus.WrittenOff);

        if (arAccountId is not null)
        {
            query = query.Where(invoice => invoice.ARAccountId == arAccountId.Value);
        }

        if (Enum.TryParse<ARInvoiceStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(invoice => invoice.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(invoice =>
                invoice.InvoiceNumber.Contains(term) ||
                (invoice.ARAccount != null && invoice.ARAccount.AccountName.Contains(term)));
        }

        var invoices = await query
            .OrderBy(invoice => invoice.ARAccount!.AccountName)
            .ThenBy(invoice => invoice.DueDate)
            .ToListAsync();

        var invoiceIds = invoices.Select(invoice => invoice.Id).ToList();
        var creditMemoTotals = invoiceIds.Count == 0
            ? new Dictionary<int, decimal>()
            : await _context.CreditMemos
                .AsNoTracking()
                .Where(memo =>
                    memo.ARInvoiceId != null &&
                    invoiceIds.Contains(memo.ARInvoiceId.Value) &&
                    memo.CreditMemoDate.Date <= asOfDate &&
                    memo.Status == MemoStatus.Applied)
                .GroupBy(memo => memo.ARInvoiceId!.Value)
                .ToDictionaryAsync(group => group.Key, group => group.Sum(memo => memo.Amount));

        var debitMemoTotals = invoiceIds.Count == 0
            ? new Dictionary<int, decimal>()
            : await _context.DebitMemos
                .AsNoTracking()
                .Where(memo =>
                    memo.ARInvoiceId != null &&
                    invoiceIds.Contains(memo.ARInvoiceId.Value) &&
                    memo.DebitMemoDate.Date <= asOfDate &&
                    memo.Status == MemoStatus.Applied)
                .GroupBy(memo => memo.ARInvoiceId!.Value)
                .ToDictionaryAsync(group => group.Key, group => group.Sum(memo => memo.Amount));

        var rows = invoices
            .Select(invoice =>
            {
                var paymentsApplied = invoice.Allocations
                    .Where(allocation => allocation.AllocationDate.Date <= asOfDate)
                    .Sum(allocation => allocation.AllocatedAmount);
                var creditMemos = creditMemoTotals.GetValueOrDefault(invoice.Id);
                var debitMemos = debitMemoTotals.GetValueOrDefault(invoice.Id);
                var balance = Math.Max(0, invoice.OriginalAmount + debitMemos - paymentsApplied - creditMemos);
                var daysOverdue = Math.Max(0, (asOfDate - invoice.DueDate.Date).Days);
                var agingBucket = GetAgingBucket(daysOverdue);
                var lastPaymentDate = invoice.Allocations
                    .Where(allocation => allocation.AllocationDate.Date <= asOfDate)
                    .Select(allocation => allocation.ARPayment != null ? (DateTime?)allocation.ARPayment.PaymentDate : allocation.AllocationDate)
                    .OrderByDescending(date => date)
                    .FirstOrDefault();

                return new ARAgingInvoiceRow(
                    invoice.ARAccountId,
                    invoice.ARAccount?.AccountName ?? "Unassigned Account",
                    invoice.InvoiceNumber,
                    invoice.InvoiceDate.Date,
                    invoice.DueDate.Date,
                    invoice.OriginalAmount,
                    paymentsApplied,
                    creditMemos,
                    debitMemos,
                    0,
                    balance,
                    agingBucket,
                    daysOverdue,
                    lastPaymentDate?.Date,
                    invoice.Status);
            })
            .Where(row => includePaid || row.Balance > 0)
            .Where(row => string.IsNullOrWhiteSpace(bucket) || row.AgingBucket.Equals(bucket, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var bucketTotals = rows
            .GroupBy(row => row.AgingBucket)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.Balance), StringComparer.OrdinalIgnoreCase);

        return new ARAgingReportResult(
            asOfDate,
            rows,
            new ARAgingSummary(
                bucketTotals.GetValueOrDefault("Current"),
                bucketTotals.GetValueOrDefault("1-30 days"),
                bucketTotals.GetValueOrDefault("31-60 days"),
                bucketTotals.GetValueOrDefault("61-90 days"),
                bucketTotals.GetValueOrDefault("Over 90 days")));
    }

    public async Task<ARCollectionReportResult> GetCollectionReportAsync(
        DateTime periodStart,
        DateTime periodEnd,
        int? arAccountId = null,
        string? status = null)
    {
        periodStart = periodStart.Date;
        periodEnd = periodEnd.Date;
        if (periodEnd < periodStart)
        {
            periodEnd = periodStart;
        }

        var opening = await GetOpenArBalanceAsOfAsync(periodStart.AddDays(-1), arAccountId);
        var endingAging = await GetAgingReportAsync(periodEnd, arAccountId, status, null, null, includePaid: false);
        var ending = endingAging.TotalBalance;
        var overdue = endingAging.Rows
            .Where(row => row.DaysOverdue > 0)
            .Sum(row => row.Balance);

        var invoiceQuery = _context.ARInvoices
            .AsNoTracking()
            .Include(invoice => invoice.ARAccount)
            .Where(invoice =>
                invoice.InvoiceDate.Date >= periodStart &&
                invoice.InvoiceDate.Date <= periodEnd &&
                invoice.Status != ARInvoiceStatus.Cancelled &&
                invoice.Status != ARInvoiceStatus.WrittenOff);

        if (arAccountId is not null)
        {
            invoiceQuery = invoiceQuery.Where(invoice => invoice.ARAccountId == arAccountId.Value);
        }

        if (Enum.TryParse<ARInvoiceStatus>(status, true, out var parsedStatus))
        {
            invoiceQuery = invoiceQuery.Where(invoice => invoice.Status == parsedStatus);
        }

        var invoices = await invoiceQuery.ToListAsync();
        var billings = invoices.Sum(invoice => invoice.OriginalAmount);
        var debitMemos = await SumDebitMemosAsync(periodStart, periodEnd, arAccountId);
        var creditMemos = await SumCreditMemosAsync(periodStart, periodEnd, arAccountId);

        var paymentAllocations = await _context.ARPaymentAllocations
            .AsNoTracking()
            .Include(allocation => allocation.ARPayment)
                .ThenInclude(payment => payment!.ARAccount)
            .Include(allocation => allocation.ARInvoice)
                .ThenInclude(invoice => invoice!.ARAccount)
            .Where(allocation =>
                allocation.AllocationDate.Date >= periodStart &&
                allocation.AllocationDate.Date <= periodEnd &&
                allocation.ARPayment != null &&
                allocation.ARInvoice != null)
            .Where(allocation => arAccountId == null || allocation.ARInvoice!.ARAccountId == arAccountId.Value)
            .ToListAsync();

        var collections = paymentAllocations.Sum(allocation => allocation.AllocatedAmount);
        var denominator = opening + billings;
        var collectionRate = denominator > 0 ? collections * 100m / denominator : (decimal?)null;
        var dayCount = Math.Max(1, (periodEnd - periodStart).Days + 1);
        var averageDailyCreditSales = billings / dayCount;
        var dso = averageDailyCreditSales > 0 ? ending / averageDailyCreditSales : (decimal?)null;

        var topCollectedAccounts = paymentAllocations
            .GroupBy(allocation => new
            {
                AccountId = allocation.ARInvoice!.ARAccountId,
                AccountName = allocation.ARInvoice.ARAccount?.AccountName ?? allocation.ARPayment!.ARAccount?.AccountName ?? "Unassigned Account"
            })
            .Select(group => new ARCollectionAccountRow(
                group.Key.AccountId,
                group.Key.AccountName,
                group.Sum(allocation => allocation.AllocatedAmount),
                group.Max(allocation => (DateTime?)(allocation.ARPayment?.PaymentDate ?? allocation.AllocationDate))))
            .OrderByDescending(row => row.Amount)
            .Take(10)
            .ToList();

        var topOverdueAccounts = endingAging.Rows
            .Where(row => row.DaysOverdue > 0)
            .GroupBy(row => new { row.ARAccountId, row.AccountName })
            .Select(group => new ARCollectionAccountRow(
                group.Key.ARAccountId,
                group.Key.AccountName,
                group.Sum(row => row.Balance),
                group.Max(row => row.LastPaymentDate)))
            .OrderByDescending(row => row.Amount)
            .Take(10)
            .ToList();

        var lastPaymentByAccount = await _context.ARPayments
            .AsNoTracking()
            .Where(payment => payment.PaymentDate.Date <= periodEnd)
            .Where(payment => arAccountId == null || payment.ARAccountId == arAccountId.Value)
            .GroupBy(payment => payment.ARAccountId)
            .Select(group => new { ARAccountId = group.Key, LastPaymentDate = group.Max(payment => payment.PaymentDate) })
            .ToListAsync();
        var recentPaymentIds = lastPaymentByAccount
            .Where(item => item.LastPaymentDate.Date >= periodEnd.AddDays(-30))
            .Select(item => item.ARAccountId)
            .ToHashSet();

        var accountsWithNoRecentPayment = endingAging.Rows
            .GroupBy(row => new { row.ARAccountId, row.AccountName })
            .Where(group => group.Sum(row => row.Balance) > 0 && !recentPaymentIds.Contains(group.Key.ARAccountId))
            .Select(group => new ARCollectionAccountRow(
                group.Key.ARAccountId,
                group.Key.AccountName,
                group.Sum(row => row.Balance),
                lastPaymentByAccount.FirstOrDefault(item => item.ARAccountId == group.Key.ARAccountId)?.LastPaymentDate.Date))
            .OrderByDescending(row => row.Amount)
            .Take(10)
            .ToList();

        var paymentsByMethod = paymentAllocations
            .GroupBy(allocation => allocation.ARPayment?.PaymentMethod ?? FinancePaymentMethod.Other)
            .Select(group => new ARPaymentMethodSummary(group.Key.ToString(), group.Sum(allocation => allocation.AllocatedAmount)))
            .OrderByDescending(row => row.Amount)
            .ToList();

        var trend = paymentAllocations
            .GroupBy(allocation => allocation.AllocationDate.Date)
            .Select(group => new ARCollectionTrendRow(group.Key, group.Sum(allocation => allocation.AllocatedAmount)))
            .OrderBy(row => row.Date)
            .ToList();

        return new ARCollectionReportResult(
            periodStart,
            periodEnd,
            opening,
            billings,
            collections,
            creditMemos,
            debitMemos,
            0,
            ending,
            overdue,
            collectionRate,
            dso,
            topCollectedAccounts,
            topOverdueAccounts,
            accountsWithNoRecentPayment,
            paymentsByMethod,
            trend);
    }

    private async Task<decimal> GetOpenArBalanceAsOfAsync(DateTime asOfDate, int? arAccountId)
    {
        var aging = await GetAgingReportAsync(asOfDate, arAccountId, null, null, null, includePaid: false);
        return aging.TotalBalance;
    }

    private async Task<decimal> SumCreditMemosAsync(DateTime start, DateTime end, int? arAccountId)
    {
        return await _context.CreditMemos
            .AsNoTracking()
            .Where(memo =>
                memo.CreditMemoDate.Date >= start &&
                memo.CreditMemoDate.Date <= end &&
                memo.Status == MemoStatus.Applied)
            .Where(memo => arAccountId == null || memo.ARAccountId == arAccountId.Value || (memo.ARInvoice != null && memo.ARInvoice.ARAccountId == arAccountId.Value))
            .SumAsync(memo => (decimal?)memo.Amount) ?? 0;
    }

    private async Task<decimal> SumDebitMemosAsync(DateTime start, DateTime end, int? arAccountId)
    {
        return await _context.DebitMemos
            .AsNoTracking()
            .Where(memo =>
                memo.DebitMemoDate.Date >= start &&
                memo.DebitMemoDate.Date <= end &&
                memo.Status == MemoStatus.Applied)
            .Where(memo => arAccountId == null || memo.ARAccountId == arAccountId.Value || (memo.ARInvoice != null && memo.ARInvoice.ARAccountId == arAccountId.Value))
            .SumAsync(memo => (decimal?)memo.Amount) ?? 0;
    }

    private static string GetAgingBucket(int daysOverdue)
    {
        return daysOverdue switch
        {
            <= 0 => "Current",
            <= 30 => "1-30 days",
            <= 60 => "31-60 days",
            <= 90 => "61-90 days",
            _ => "Over 90 days"
        };
    }
}

public sealed record ARAgingReportResult(
    DateTime AsOfDate,
    IReadOnlyList<ARAgingInvoiceRow> Rows,
    ARAgingSummary Summary)
{
    public decimal TotalBalance => Rows.Sum(row => row.Balance);
}

public sealed record ARAgingInvoiceRow(
    int ARAccountId,
    string AccountName,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal OriginalAmount,
    decimal PaymentsApplied,
    decimal CreditMemos,
    decimal DebitMemos,
    decimal Adjustments,
    decimal Balance,
    string AgingBucket,
    int DaysOverdue,
    DateTime? LastPaymentDate,
    ARInvoiceStatus Status);

public sealed record ARAgingSummary(
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Over90)
{
    public decimal Total => Current + Days1To30 + Days31To60 + Days61To90 + Over90;
}

public sealed record ARCollectionReportResult(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal OpeningAR,
    decimal Billings,
    decimal Collections,
    decimal CreditMemos,
    decimal DebitMemos,
    decimal Adjustments,
    decimal EndingAR,
    decimal OverdueAR,
    decimal? CollectionRate,
    decimal? DaysSalesOutstanding,
    IReadOnlyList<ARCollectionAccountRow> TopCollectedAccounts,
    IReadOnlyList<ARCollectionAccountRow> TopOverdueAccounts,
    IReadOnlyList<ARCollectionAccountRow> AccountsWithNoRecentPayment,
    IReadOnlyList<ARPaymentMethodSummary> PaymentsByMethod,
    IReadOnlyList<ARCollectionTrendRow> CollectionTrend);

public sealed record ARCollectionAccountRow(
    int ARAccountId,
    string AccountName,
    decimal Amount,
    DateTime? LastPaymentDate);

public sealed record ARPaymentMethodSummary(string PaymentMethod, decimal Amount);

public sealed record ARCollectionTrendRow(DateTime Date, decimal Collections);
