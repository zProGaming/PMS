using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public class CashFlowReportService(ApplicationDbContext context, AuditLogService auditLogService)
{
    private const string OtherOperatingCode = "OP_OTHER";

    public async Task<CashFlowStatementResult> GenerateStatementAsync(DateTime startDate, DateTime endDate, CashFlowMethod method = CashFlowMethod.Direct)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;
        if (endDate < startDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var warnings = new List<string>();
        var cashAccountIds = await GetActiveCashAccountIdsAsync();
        if (cashAccountIds.Count == 0)
        {
            warnings.Add("Cash accounts are not configured yet. Go to Accounting > Cash Account Settings before final reporting.");
        }

        var beginningCash = await GetCashBalanceBeforeAsync(startDate, cashAccountIds);
        var endingCash = await GetCashBalanceBeforeAsync(endDate.AddDays(1), cashAccountIds);
        var movements = method == CashFlowMethod.Direct
            ? await GetCashMovementsAsync(startDate, endDate)
            : new List<CashMovementRow>();

        if (method == CashFlowMethod.Indirect)
        {
            warnings.Add("Indirect method is not currently enabled. Direct method is available for configured cash-flow mappings.");
        }

        var lines = await BuildStatementLinesAsync(beginningCash, endingCash, movements);
        var operating = movements.Where(row => row.CashFlowSection == CashFlowSection.Operating).Sum(row => row.Amount);
        var investing = movements.Where(row => row.CashFlowSection == CashFlowSection.Investing).Sum(row => row.Amount);
        var financing = movements.Where(row => row.CashFlowSection == CashFlowSection.Financing).Sum(row => row.Amount);
        var netChange = operating + investing + financing;
        var unmappedCount = movements.Count(row => !row.IsMapped);

        if (unmappedCount > 0)
        {
            warnings.Add($"{unmappedCount:N0} cash movement item(s) are using the default Other Operating Cash Flows classification.");
        }

        var unreconciledBankTransactions = await context.BankTransactions
            .AsNoTracking()
            .CountAsync(transaction =>
                !transaction.IsReconciled &&
                transaction.TransactionDate >= startDate &&
                transaction.TransactionDate < endDate.AddDays(1));
        if (unreconciledBankTransactions > 0)
        {
            warnings.Add("Cash flow values should be reviewed against bank reconciliations before final reporting.");
        }

        var openPeriod = await context.AccountingPeriods
            .AsNoTracking()
            .Where(period => period.Status == AccountingPeriodStatus.Open && period.StartDate <= endDate && period.EndDate >= startDate)
            .OrderBy(period => period.StartDate)
            .Select(period => period.PeriodName)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(openPeriod))
        {
            warnings.Add($"Accounting period {openPeriod} is still open. Report values may still change.");
        }

        var reconciliationDifference = endingCash - (beginningCash + netChange);
        if (reconciliationDifference != 0)
        {
            warnings.Add("Cash flow movement does not reconcile exactly with ending cash balance. Review cash-to-cash transfers and mappings.");
        }

        return new CashFlowStatementResult(
            startDate,
            endDate,
            method,
            beginningCash,
            operating,
            investing,
            financing,
            netChange,
            endingCash,
            reconciliationDifference,
            lines,
            movements,
            unmappedCount,
            unreconciledBankTransactions,
            warnings);
    }

    public async Task<IList<CashMovementRow>> GetCashMovementsAsync(
        DateTime startDate,
        DateTime endDate,
        int? cashAccountId = null,
        CashFlowSection? section = null,
        int? categoryId = null,
        SourceModule? sourceModule = null,
        bool? mapped = null)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;
        if (endDate < startDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var endExclusive = endDate.AddDays(1);
        var cashAccountIds = await GetActiveCashAccountIdsAsync();
        if (cashAccountId is not null)
        {
            cashAccountIds = cashAccountIds.Contains(cashAccountId.Value) ? [cashAccountId.Value] : [];
        }

        if (cashAccountIds.Count == 0)
        {
            return [];
        }

        var categories = await context.CashFlowCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .ToDictionaryAsync(category => category.Id);
        var otherOperating = await GetOtherOperatingCategoryAsync(categories);
        var rules = await context.CashFlowMappingRules
            .AsNoTracking()
            .Include(rule => rule.CashFlowCategory)
            .Where(rule => rule.IsActive && rule.CashFlowCategory != null && rule.CashFlowCategory.IsActive)
            .ToListAsync();

        var entries = await context.JournalEntries
            .AsNoTracking()
            .Include(entry => entry.Lines)
                .ThenInclude(line => line.GLAccount)
            .Where(entry =>
                entry.Status == JournalEntryStatus.Posted &&
                entry.JournalDate >= startDate &&
                entry.JournalDate < endExclusive &&
                entry.Lines.Any(line => cashAccountIds.Contains(line.GLAccountId)))
            .OrderBy(entry => entry.JournalDate)
            .ThenBy(entry => entry.Id)
            .ToListAsync();

        var rows = new List<CashMovementRow>();
        foreach (var entry in entries)
        {
            var cashLines = entry.Lines.Where(line => cashAccountIds.Contains(line.GLAccountId)).ToList();
            var nonCashLines = entry.Lines.Where(line => !cashAccountIds.Contains(line.GLAccountId)).ToList();
            if (nonCashLines.Count == 0)
            {
                continue;
            }

            foreach (var cashLine in cashLines)
            {
                var amount = cashLine.DebitAmount - cashLine.CreditAmount;
                if (amount == 0)
                {
                    continue;
                }

                var offset = SelectOffsetLine(nonCashLines, amount);
                var classification = Classify(entry, offset, rules, otherOperating);
                rows.Add(new CashMovementRow(
                    entry.Id,
                    entry.JournalDate,
                    entry.JournalNumber,
                    entry.SourceModule,
                    entry.SourceTransactionType,
                    cashLine.GLAccountId,
                    cashLine.GLAccount?.AccountCode ?? string.Empty,
                    cashLine.GLAccount?.AccountName ?? "Cash Account",
                    offset?.GLAccountId,
                    offset?.GLAccount?.AccountCode ?? string.Empty,
                    offset?.GLAccount?.AccountName ?? "Multiple or unmapped offset accounts",
                    cashLine.Description ?? entry.Description,
                    amount > 0 ? amount : 0,
                    amount < 0 ? Math.Abs(amount) : 0,
                    amount,
                    classification.Section,
                    classification.CategoryId,
                    classification.CategoryCode,
                    classification.CategoryName,
                    classification.IsMapped));
            }
        }

        if (section is not null)
        {
            rows = rows.Where(row => row.CashFlowSection == section.Value).ToList();
        }

        if (categoryId is not null)
        {
            rows = rows.Where(row => row.CashFlowCategoryId == categoryId.Value).ToList();
        }

        if (sourceModule is not null)
        {
            rows = rows.Where(row => row.SourceModule == sourceModule.Value).ToList();
        }

        if (mapped is not null)
        {
            rows = rows.Where(row => row.IsMapped == mapped.Value).ToList();
        }

        return rows;
    }

    public async Task<CashFlowReportSnapshot> SaveSnapshotAsync(DateTime startDate, DateTime endDate, CashFlowMethod method, string? generatedBy)
    {
        var result = await GenerateStatementAsync(startDate, endDate, method);
        var snapshot = new CashFlowReportSnapshot
        {
            ReportName = method == CashFlowMethod.Direct ? "Statement of Cash Flows - Direct Method" : "Statement of Cash Flows - Indirect Method Placeholder",
            PeriodStart = result.PeriodStart,
            PeriodEnd = result.PeriodEnd,
            BeginningCashBalance = result.BeginningCashBalance,
            NetCashFromOperatingActivities = result.NetCashFromOperatingActivities,
            NetCashFromInvestingActivities = result.NetCashFromInvestingActivities,
            NetCashFromFinancingActivities = result.NetCashFromFinancingActivities,
            NetIncreaseDecreaseInCash = result.NetIncreaseDecreaseInCash,
            EndingCashBalance = result.EndingCashBalance,
            GeneratedAt = DateTime.Now,
            GeneratedBy = generatedBy,
            Notes = "Generated from configured cash flow mappings and posted journal entries."
        };

        foreach (var line in result.Lines)
        {
            snapshot.Lines.Add(new CashFlowReportSnapshotLine
            {
                CashFlowSection = line.CashFlowSection,
                CashFlowCategoryId = line.CashFlowCategoryId,
                LineCode = line.LineCode,
                LineName = line.LineName,
                Amount = line.Amount,
                SortOrder = line.SortOrder,
                IsSubtotal = line.IsSubtotal
            });
        }

        context.CashFlowReportSnapshots.Add(snapshot);
        await context.SaveChangesAsync();

        await auditLogService.LogAsync(
            AuditActionType.Create,
            "Accounting",
            nameof(CashFlowReportSnapshot),
            snapshot.Id.ToString(),
            null,
            new
            {
                snapshot.ReportName,
                snapshot.PeriodStart,
                snapshot.PeriodEnd,
                snapshot.EndingCashBalance,
                Method = method.ToString()
            },
            generatedBy);

        return snapshot;
    }

    public async Task<int> CountUnmappedAsync(DateTime startDate, DateTime endDate)
    {
        var rows = await GetCashMovementsAsync(startDate, endDate, mapped: false);
        return rows.Count;
    }

    private async Task<IList<int>> GetActiveCashAccountIdsAsync()
    {
        return await context.CashAccountSettings
            .AsNoTracking()
            .Where(setting => setting.IsActive && setting.GLAccount != null && setting.GLAccount.IsActive)
            .Select(setting => setting.GLAccountId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<decimal> GetCashBalanceBeforeAsync(DateTime beforeDate, IList<int> cashAccountIds)
    {
        if (cashAccountIds.Count == 0)
        {
            return 0;
        }

        return await context.JournalEntryLines
            .AsNoTracking()
            .Where(line =>
                line.JournalEntry != null &&
                line.JournalEntry.Status == JournalEntryStatus.Posted &&
                line.JournalEntry.JournalDate < beforeDate &&
                cashAccountIds.Contains(line.GLAccountId))
            .SumAsync(line => line.DebitAmount - line.CreditAmount);
    }

    private async Task<IList<CashFlowStatementLine>> BuildStatementLinesAsync(decimal beginningCash, decimal endingCash, IList<CashMovementRow> movements)
    {
        var categories = await context.CashFlowCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.CashFlowSection)
            .ThenBy(category => category.SortOrder)
            .ToListAsync();

        var lines = new List<CashFlowStatementLine>();
        foreach (var section in new[] { CashFlowSection.Operating, CashFlowSection.Investing, CashFlowSection.Financing })
        {
            var sectionCategories = categories.Where(category => category.CashFlowSection == section && !category.IsSubtotal).ToList();
            foreach (var category in sectionCategories)
            {
                var amount = movements.Where(row => row.CashFlowCategoryId == category.Id).Sum(row => row.Amount);
                lines.Add(new CashFlowStatementLine(section, category.Id, category.Code, category.Name, amount, category.SortOrder, false));
            }

            var subtotal = movements.Where(row => row.CashFlowSection == section).Sum(row => row.Amount);
            var subtotalCategory = categories.FirstOrDefault(category => category.CashFlowSection == section && category.IsSubtotal);
            lines.Add(new CashFlowStatementLine(
                section,
                subtotalCategory?.Id,
                subtotalCategory?.Code ?? $"{section.ToString().ToUpperInvariant()}_NET",
                subtotalCategory?.Name ?? $"Net Cash from {section} Activities",
                subtotal,
                900,
                true));
        }

        var netChange = movements.Sum(row => row.Amount);
        var beginning = categories.FirstOrDefault(category => category.CashFlowSection == CashFlowSection.BeginningCash);
        var change = categories.FirstOrDefault(category => category.Code == "REC_CHANGE");
        var ending = categories.FirstOrDefault(category => category.CashFlowSection == CashFlowSection.EndingCash);
        lines.Add(new CashFlowStatementLine(CashFlowSection.BeginningCash, beginning?.Id, beginning?.Code ?? "BEGINNING_CASH", beginning?.Name ?? "Beginning Cash and Cash Equivalents", beginningCash, 1000, true));
        lines.Add(new CashFlowStatementLine(CashFlowSection.Reconciliation, change?.Id, change?.Code ?? "NET_CHANGE", change?.Name ?? "Net Increase or Decrease in Cash", netChange, 1010, true));
        lines.Add(new CashFlowStatementLine(CashFlowSection.EndingCash, ending?.Id, ending?.Code ?? "ENDING_CASH", ending?.Name ?? "Ending Cash and Cash Equivalents", endingCash, 1020, true));
        return lines;
    }

    private static JournalEntryLine? SelectOffsetLine(IList<JournalEntryLine> nonCashLines, decimal cashAmount)
    {
        return cashAmount > 0
            ? nonCashLines.OrderByDescending(line => line.CreditAmount).FirstOrDefault()
            : nonCashLines.OrderByDescending(line => line.DebitAmount).FirstOrDefault();
    }

    private static CashFlowClassification Classify(
        JournalEntry entry,
        JournalEntryLine? offset,
        IList<CashFlowMappingRule> rules,
        CashFlowCategory otherOperating)
    {
        var sourceRule = rules.FirstOrDefault(rule =>
            rule.SourceModule == entry.SourceModule &&
            rule.SourceTransactionType == entry.SourceTransactionType);
        sourceRule ??= rules.FirstOrDefault(rule =>
            rule.SourceModule == entry.SourceModule &&
            rule.SourceTransactionType is null);

        var accountRule = offset is null
            ? null
            : rules.FirstOrDefault(rule => rule.GLAccountId == offset.GLAccountId);

        var rule = sourceRule ?? accountRule;
        var category = rule?.CashFlowCategory ?? otherOperating;
        return new CashFlowClassification(
            category.CashFlowSection,
            category.Id,
            category.Code,
            category.Name,
            rule is not null);
    }

    private async Task<CashFlowCategory> GetOtherOperatingCategoryAsync(IReadOnlyDictionary<int, CashFlowCategory> categories)
    {
        var other = categories.Values.FirstOrDefault(category => category.Code == OtherOperatingCode);
        if (other is not null)
        {
            return other;
        }

        other = new CashFlowCategory
        {
            Code = OtherOperatingCode,
            Name = "Other Operating Cash Flows",
            CashFlowSection = CashFlowSection.Operating,
            SortOrder = 110,
            IsActive = true
        };
        context.CashFlowCategories.Add(other);
        await context.SaveChangesAsync();
        return other;
    }
}

public record CashFlowStatementResult(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    CashFlowMethod Method,
    decimal BeginningCashBalance,
    decimal NetCashFromOperatingActivities,
    decimal NetCashFromInvestingActivities,
    decimal NetCashFromFinancingActivities,
    decimal NetIncreaseDecreaseInCash,
    decimal EndingCashBalance,
    decimal ReconciliationDifference,
    IList<CashFlowStatementLine> Lines,
    IList<CashMovementRow> Movements,
    int UnmappedItemCount,
    int UnreconciledBankTransactionCount,
    IList<string> Warnings);

public record CashFlowStatementLine(
    CashFlowSection CashFlowSection,
    int? CashFlowCategoryId,
    string LineCode,
    string LineName,
    decimal Amount,
    int SortOrder,
    bool IsSubtotal);

public record CashMovementRow(
    int JournalEntryId,
    DateTime JournalDate,
    string JournalNumber,
    SourceModule SourceModule,
    SourceTransactionType SourceTransactionType,
    int CashAccountId,
    string CashAccountCode,
    string CashAccountName,
    int? OffsetAccountId,
    string OffsetAccountCode,
    string OffsetAccountName,
    string Description,
    decimal CashInflow,
    decimal CashOutflow,
    decimal Amount,
    CashFlowSection CashFlowSection,
    int? CashFlowCategoryId,
    string CashFlowCategoryCode,
    string CashFlowCategoryName,
    bool IsMapped)
{
    public string MappingStatus => IsMapped ? "Mapped" : "Unmapped";
}

internal record CashFlowClassification(
    CashFlowSection Section,
    int CategoryId,
    string CategoryCode,
    string CategoryName,
    bool IsMapped);
