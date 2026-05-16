using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting;

public class IndexModel(ApplicationDbContext context, CashFlowReportService cashFlowReportService) : PageModel
{
    public int UnpostedTransactions { get; private set; }

    public int JournalEntriesToday { get; private set; }

    public decimal TrialBalanceDifference { get; private set; }

    public string OpenPeriodName { get; private set; } = "None";

    public int PendingPostingBatches { get; private set; }

    public string LatestPostedBatch { get; private set; } = "None";

    public int ActivePostingRules { get; private set; }

    public int ActiveGLAccounts { get; private set; }

    public decimal APBalance { get; private set; }

    public int PaymentVouchersForRelease { get; private set; }

    public int BankReconciliationsPending { get; private set; }

    public decimal BeginningCash { get; private set; }

    public decimal OperatingCashFlow { get; private set; }

    public decimal InvestingCashFlow { get; private set; }

    public decimal FinancingCashFlow { get; private set; }

    public decimal NetCashChange { get; private set; }

    public decimal EndingCash { get; private set; }

    public int UnmappedCashFlowItems { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        JournalEntriesToday = await context.JournalEntries.AsNoTracking().CountAsync(entry => entry.CreatedAt >= today);
        ActivePostingRules = await context.PostingRules.AsNoTracking().CountAsync(rule => rule.IsActive);
        ActiveGLAccounts = await context.GLAccounts.AsNoTracking().CountAsync(account => account.IsActive);
        APBalance = await context.APInvoices.AsNoTracking()
            .Where(invoice => invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
            .SumAsync(invoice => invoice.Balance);
        PaymentVouchersForRelease = await context.PaymentVouchers.AsNoTracking().CountAsync(voucher => voucher.Status == PaymentVoucherStatus.Approved);
        BankReconciliationsPending = await context.BankReconciliations.AsNoTracking().CountAsync(reconciliation => reconciliation.Status != BankReconciliationStatus.Approved && reconciliation.Status != BankReconciliationStatus.Cancelled);
        PendingPostingBatches = await context.PostingBatches.AsNoTracking().CountAsync(batch => batch.Status == PostingBatchStatus.Draft || batch.Status == PostingBatchStatus.Processing);
        LatestPostedBatch = await context.PostingBatches
            .AsNoTracking()
            .Where(batch => batch.Status == PostingBatchStatus.Posted || batch.Status == PostingBatchStatus.PostedWithErrors)
            .OrderByDescending(batch => batch.PostedAt)
            .Select(batch => batch.BatchNumber)
            .FirstOrDefaultAsync() ?? "None";
        OpenPeriodName = await context.AccountingPeriods
            .AsNoTracking()
            .Where(period => period.Status == AccountingPeriodStatus.Open && period.StartDate <= today && period.EndDate >= today)
            .Select(period => period.PeriodName)
            .FirstOrDefaultAsync() ?? "None";

        var postedLines = await context.JournalEntryLines
            .AsNoTracking()
            .Where(line => line.JournalEntry != null && line.JournalEntry.Status == JournalEntryStatus.Posted)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Debit = group.Sum(line => line.DebitAmount),
                Credit = group.Sum(line => line.CreditAmount)
            })
            .FirstOrDefaultAsync();
        TrialBalanceDifference = (postedLines?.Debit ?? 0) - (postedLines?.Credit ?? 0);

        var unpostedFolioItems = await context.FolioItems.AsNoTracking().CountAsync(item => !item.IsVoided && !context.JournalEntries.Any(entry =>
            entry.Status == JournalEntryStatus.Posted &&
            (entry.SourceTransactionType == SourceTransactionType.FolioCharge || entry.SourceTransactionType == SourceTransactionType.RoomCharge) &&
            entry.SourceReferenceId == item.Id));
        var unpostedPayments = await context.Payments.AsNoTracking().CountAsync(payment => payment.Status == PaymentStatus.Completed && !context.JournalEntries.Any(entry =>
            entry.Status == JournalEntryStatus.Posted &&
            entry.SourceTransactionType == SourceTransactionType.FolioPayment &&
            entry.SourceReferenceId == payment.Id));
        var unpostedPos = await context.POSOrders.AsNoTracking().CountAsync(order => order.OrderStatus == POSOrderStatus.Closed && order.PaymentStatus != POSPaymentStatus.Voided && !context.JournalEntries.Any(entry =>
            entry.Status == JournalEntryStatus.Posted &&
            (entry.SourceTransactionType == SourceTransactionType.POSPayment || entry.SourceTransactionType == SourceTransactionType.POSChargeToRoom) &&
            entry.SourceReferenceId == order.Id));
        var unpostedReceiving = await context.ReceivingRecords.AsNoTracking().CountAsync(record => record.Status == ReceivingStatus.Posted && !context.JournalEntries.Any(entry =>
            entry.Status == JournalEntryStatus.Posted &&
            entry.SourceTransactionType == SourceTransactionType.PurchaseReceiving &&
            entry.SourceReferenceId == record.Id));

        UnpostedTransactions = unpostedFolioItems + unpostedPayments + unpostedPos + unpostedReceiving;

        var cashFlow = await cashFlowReportService.GenerateStatementAsync(monthStart, today);
        BeginningCash = cashFlow.BeginningCashBalance;
        OperatingCashFlow = cashFlow.NetCashFromOperatingActivities;
        InvestingCashFlow = cashFlow.NetCashFromInvestingActivities;
        FinancingCashFlow = cashFlow.NetCashFromFinancingActivities;
        NetCashChange = cashFlow.NetIncreaseDecreaseInCash;
        EndingCash = cashFlow.EndingCashBalance;
        UnmappedCashFlowItems = cashFlow.UnmappedItemCount;
    }
}
