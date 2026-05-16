using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Services;

public class AccountingReportService(ApplicationDbContext context)
{
    public async Task<IList<AccountBalanceRow>> GetAccountBalancesAsync(DateTime startDate, DateTime endDate)
    {
        var endExclusive = endDate.Date.AddDays(1);
        var rows = await context.JournalEntryLines
            .AsNoTracking()
            .Where(line =>
                line.JournalEntry != null &&
                line.JournalEntry.Status == JournalEntryStatus.Posted &&
                line.JournalEntry.JournalDate >= startDate.Date &&
                line.JournalEntry.JournalDate < endExclusive)
            .Select(line => new
            {
                line.GLAccountId,
                line.GLAccount!.AccountCode,
                line.GLAccount.AccountName,
                line.GLAccount.AccountType,
                line.GLAccount.NormalBalance,
                line.GLAccount.UsaliDepartmentId,
                UsaliDepartmentName = line.GLAccount.UsaliDepartment != null ? line.GLAccount.UsaliDepartment.Name : null,
                line.GLAccount.UsaliReportLineId,
                UsaliReportLineName = line.GLAccount.UsaliReportLine != null ? line.GLAccount.UsaliReportLine.Name : null,
                line.DebitAmount,
                line.CreditAmount
            })
            .GroupBy(line => new
            {
                line.GLAccountId,
                line.AccountCode,
                line.AccountName,
                line.AccountType,
                line.NormalBalance,
                line.UsaliDepartmentId,
                line.UsaliDepartmentName,
                line.UsaliReportLineId,
                line.UsaliReportLineName
            })
            .Select(group => new
            {
                group.Key.GLAccountId,
                group.Key.AccountCode,
                group.Key.AccountName,
                group.Key.AccountType,
                group.Key.NormalBalance,
                group.Key.UsaliDepartmentId,
                group.Key.UsaliDepartmentName,
                group.Key.UsaliReportLineId,
                group.Key.UsaliReportLineName,
                DebitAmount = group.Sum(line => line.DebitAmount),
                CreditAmount = group.Sum(line => line.CreditAmount)
            })
            .OrderBy(row => row.AccountCode)
            .ToListAsync();

        return rows
            .Select(row => new AccountBalanceRow(
                row.GLAccountId,
                row.AccountCode,
                row.AccountName,
                row.AccountType,
                row.NormalBalance,
                row.UsaliDepartmentId,
                row.UsaliDepartmentName,
                row.UsaliReportLineId,
                row.UsaliReportLineName,
                row.DebitAmount,
                row.CreditAmount))
            .ToList();
    }

    public async Task<IList<LedgerLineRow>> GetLedgerLinesAsync(DateTime startDate, DateTime endDate, int? glAccountId)
    {
        var endExclusive = endDate.Date.AddDays(1);
        var query = context.JournalEntryLines
            .AsNoTracking()
            .Where(line =>
                line.JournalEntry != null &&
                line.GLAccount != null &&
                line.JournalEntry.Status == JournalEntryStatus.Posted &&
                line.JournalEntry.JournalDate >= startDate.Date &&
                line.JournalEntry.JournalDate < endExclusive);

        if (glAccountId is not null)
        {
            query = query.Where(line => line.GLAccountId == glAccountId.Value);
        }

        return await query
            .OrderBy(line => line.JournalEntry!.JournalDate)
            .ThenBy(line => line.JournalEntryId)
            .Select(line => new LedgerLineRow(
                line.JournalEntry!.JournalDate,
                line.JournalEntry.JournalNumber,
                line.GLAccount!.AccountCode,
                line.GLAccount.AccountName,
                line.Description ?? line.JournalEntry.Description,
                line.DebitAmount,
                line.CreditAmount))
            .ToListAsync();
    }

    public decimal NetForTypes(IEnumerable<AccountBalanceRow> rows, params GLAccountType[] accountTypes)
    {
        return rows.Where(row => accountTypes.Contains(row.AccountType)).Sum(row => row.NetAmount);
    }

    public decimal CreditNormalAmount(IEnumerable<AccountBalanceRow> rows, params GLAccountType[] accountTypes)
    {
        return rows.Where(row => accountTypes.Contains(row.AccountType)).Sum(row => row.CreditAmount - row.DebitAmount);
    }

    public decimal DebitNormalAmount(IEnumerable<AccountBalanceRow> rows, params GLAccountType[] accountTypes)
    {
        return rows.Where(row => accountTypes.Contains(row.AccountType)).Sum(row => row.DebitAmount - row.CreditAmount);
    }
}

public record AccountBalanceRow(
    int GLAccountId,
    string AccountCode,
    string AccountName,
    GLAccountType AccountType,
    NormalBalance NormalBalance,
    int? UsaliDepartmentId,
    string? UsaliDepartmentName,
    int? UsaliReportLineId,
    string? UsaliReportLineName,
    decimal DebitAmount,
    decimal CreditAmount)
{
    public decimal DebitBalance => DebitAmount > CreditAmount ? DebitAmount - CreditAmount : 0;

    public decimal CreditBalance => CreditAmount > DebitAmount ? CreditAmount - DebitAmount : 0;

    public decimal NetAmount => NormalBalance == NormalBalance.Debit ? DebitAmount - CreditAmount : CreditAmount - DebitAmount;
}

public record LedgerLineRow(
    DateTime JournalDate,
    string JournalNumber,
    string AccountCode,
    string AccountName,
    string Description,
    decimal DebitAmount,
    decimal CreditAmount);
