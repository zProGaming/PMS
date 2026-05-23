using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance;

public class IndexModel(ApplicationDbContext context, ARCollectionReportService arCollectionReportService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly ARCollectionReportService _arCollectionReportService = arCollectionReportService;

    public decimal TotalPaymentsToday { get; set; }
    public decimal CashPaymentsToday { get; set; }
    public decimal CardEWalletPaymentsToday { get; set; }
    public int OpenCashierShifts { get; set; }
    public int RefundsPendingApproval { get; set; }
    public int VoidRequestsPendingApproval { get; set; }
    public int DiscountsPendingApproval { get; set; }
    public decimal ARBalanceTotal { get; set; }
    public decimal OverdueARTotal { get; set; }
    public int ARInvoicesDueWithin7Days { get; set; }
    public decimal ARCollectionsToday { get; set; }
    public decimal ARCollectionsThisWeek { get; set; }
    public decimal ARCollectionsThisMonth { get; set; }
    public decimal? ARCollectionRateThisMonth { get; set; }
    public IList<ARCollectionAccountRow> TopOverdueAccounts { get; set; } = new List<ARCollectionAccountRow>();
    public IList<ARCollectionAccountRow> AccountsWithNoPaymentIn30Days { get; set; } = new List<ARCollectionAccountRow>();
    public int UnpostedTransactions { get; set; }
    public int JournalEntriesToday { get; set; }
    public string TrialBalanceStatus { get; set; } = "Balanced";
    public string OpenAccountingPeriod { get; set; } = "None";
    public int PendingPostingBatches { get; set; }
    public string LatestPostedBatch { get; set; } = "None";
    public decimal APBalance { get; set; }
    public decimal APOverdue { get; set; }
    public int PaymentVouchersPendingApproval { get; set; }
    public int PaymentVouchersForRelease { get; set; }
    public int BankReconciliationsPending { get; set; }
    public int MonthEndChecklistProgress { get; set; }
    public int PayrollPeriodsPendingApproval { get; set; }
    public int PayrollPeriodsPendingPosting { get; set; }
    public decimal LaborCostThisMonth { get; set; }
    public int DepartmentsOverLaborBudget { get; set; }
    public int ServiceChargePoolsPendingApproval { get; set; }
    public int ServiceChargePoolsPendingPosting { get; set; }
    public int OpenGroupFolios { get; set; }
    public decimal GroupDepositsReceived { get; set; }
    public decimal GroupOutstandingBalances { get; set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var dueSoon = today.AddDays(7);
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var completedPaymentsToday = _context.Payments
            .AsNoTracking()
            .Where(payment => payment.PaymentDate >= today && payment.PaymentDate < tomorrow && payment.Status == PaymentStatus.Completed);

        TotalPaymentsToday = await completedPaymentsToday.SumAsync(payment => payment.Amount);
        CashPaymentsToday = await completedPaymentsToday
            .Where(payment => payment.PaymentMethod.ToUpper() == "CASH")
            .SumAsync(payment => payment.Amount);
        CardEWalletPaymentsToday = await completedPaymentsToday
            .Where(payment =>
                payment.PaymentMethod.ToUpper().Contains("CARD") ||
                payment.PaymentMethod.ToUpper().Contains("EWALLET") ||
                payment.PaymentMethod.ToUpper().Contains("GCASH") ||
                payment.PaymentMethod.ToUpper().Contains("MAYA"))
            .SumAsync(payment => payment.Amount);

        OpenCashierShifts = await _context.CashierShifts.CountAsync(shift => shift.Status == CashierShiftStatus.Open);
        RefundsPendingApproval = await _context.RefundTransactions.CountAsync(refund => refund.Status == RefundStatus.Requested || refund.Status == RefundStatus.ForApproval);
        VoidRequestsPendingApproval = await _context.VoidRequests.CountAsync(request => request.Status == ApprovalStatus.Pending);
        DiscountsPendingApproval = await _context.DiscountApprovals.CountAsync(discount => discount.Status == ApprovalStatus.Pending);
        ARBalanceTotal = await _context.ARAccounts.SumAsync(account => account.CurrentBalance);
        OverdueARTotal = await _context.ARInvoices
            .Where(invoice => invoice.DueDate < today && invoice.Balance > 0 && invoice.Status != ARInvoiceStatus.Cancelled && invoice.Status != ARInvoiceStatus.WrittenOff)
            .SumAsync(invoice => invoice.Balance);
        ARInvoicesDueWithin7Days = await _context.ARInvoices
            .CountAsync(invoice => invoice.DueDate >= today && invoice.DueDate <= dueSoon && invoice.Balance > 0);
        var dailyAr = await _arCollectionReportService.GetCollectionReportAsync(today, today);
        var weeklyAr = await _arCollectionReportService.GetCollectionReportAsync(weekStart, today);
        var monthlyAr = await _arCollectionReportService.GetCollectionReportAsync(monthStart, today);
        ARCollectionsToday = dailyAr.Collections;
        ARCollectionsThisWeek = weeklyAr.Collections;
        ARCollectionsThisMonth = monthlyAr.Collections;
        ARCollectionRateThisMonth = monthlyAr.CollectionRate;
        TopOverdueAccounts = monthlyAr.TopOverdueAccounts.Take(5).ToList();
        AccountsWithNoPaymentIn30Days = monthlyAr.AccountsWithNoRecentPayment.Take(5).ToList();

        JournalEntriesToday = await _context.JournalEntries.CountAsync(entry => entry.CreatedAt >= today);
        PendingPostingBatches = await _context.PostingBatches.CountAsync(batch => batch.Status == PostingBatchStatus.Draft || batch.Status == PostingBatchStatus.Processing);
        LatestPostedBatch = await _context.PostingBatches
            .Where(batch => batch.Status == PostingBatchStatus.Posted || batch.Status == PostingBatchStatus.PostedWithErrors)
            .OrderByDescending(batch => batch.PostedAt)
            .Select(batch => batch.BatchNumber)
            .FirstOrDefaultAsync() ?? "None";
        OpenAccountingPeriod = await _context.AccountingPeriods
            .Where(period => period.Status == AccountingPeriodStatus.Open && period.StartDate <= today && period.EndDate >= today)
            .Select(period => period.PeriodName)
            .FirstOrDefaultAsync() ?? "None";
        var trialBalance = await _context.JournalEntryLines
            .Where(line => line.JournalEntry != null && line.JournalEntry.Status == JournalEntryStatus.Posted)
            .GroupBy(_ => 1)
            .Select(group => new { Debit = group.Sum(line => line.DebitAmount), Credit = group.Sum(line => line.CreditAmount) })
            .FirstOrDefaultAsync();
        TrialBalanceStatus = (trialBalance?.Debit ?? 0) == (trialBalance?.Credit ?? 0) ? "Balanced" : "Out of balance";
        UnpostedTransactions = await _context.FolioItems.CountAsync(item => !item.IsVoided && (item.ChargeCode != "FB" || !item.Description.Contains("Order #")) && !_context.JournalEntries.Any(entry => entry.Status == JournalEntryStatus.Posted && entry.SourceReferenceId == item.Id && (entry.SourceTransactionType == SourceTransactionType.FolioCharge || entry.SourceTransactionType == SourceTransactionType.RoomCharge)))
            + await _context.Payments.CountAsync(payment => payment.Status == PaymentStatus.Completed && !_context.JournalEntries.Any(entry => entry.Status == JournalEntryStatus.Posted && entry.SourceReferenceId == payment.Id && entry.SourceTransactionType == SourceTransactionType.FolioPayment));

        APBalance = await _context.APInvoices
            .Where(invoice => invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
            .SumAsync(invoice => invoice.Balance);
        APOverdue = await _context.APInvoices
            .Where(invoice => invoice.DueDate < today && invoice.Balance > 0 && invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
            .SumAsync(invoice => invoice.Balance);
        PaymentVouchersPendingApproval = await _context.PaymentVouchers
            .CountAsync(voucher => voucher.Status == PaymentVoucherStatus.ForApproval || voucher.Status == PaymentVoucherStatus.Draft);
        PaymentVouchersForRelease = await _context.PaymentVouchers
            .CountAsync(voucher => voucher.Status == PaymentVoucherStatus.Approved);
        BankReconciliationsPending = await _context.BankReconciliations
            .CountAsync(reconciliation => reconciliation.Status != BankReconciliationStatus.Approved && reconciliation.Status != BankReconciliationStatus.Cancelled);
        var currentPeriodId = await _context.AccountingPeriods
            .Where(period => period.Status == AccountingPeriodStatus.Open && period.StartDate <= today && period.EndDate >= today)
            .Select(period => (int?)period.Id)
            .FirstOrDefaultAsync();
        if (currentPeriodId is not null)
        {
            var checklist = await _context.MonthEndCloseChecklists
                .Where(item => item.AccountingPeriodId == currentPeriodId.Value)
                .ToListAsync();
            MonthEndChecklistProgress = checklist.Count == 0
                ? 0
                : (int)Math.Round(checklist.Count(item => item.Status is MonthEndChecklistStatus.Completed or MonthEndChecklistStatus.NotApplicable) * 100m / checklist.Count);
        }

        PayrollPeriodsPendingApproval = await _context.PayrollPeriods.CountAsync(period => period.Status == PayrollPeriodStatus.ForApproval);
        PayrollPeriodsPendingPosting = await _context.PayrollPeriods.CountAsync(period => period.Status == PayrollPeriodStatus.Approved);
        LaborCostThisMonth = await _context.PayrollCostEntries
            .Where(entry => entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate < nextMonth &&
                entry.PayrollPeriod.EndDate >= monthStart &&
                entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled)
            .SumAsync(entry => entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay);
        ServiceChargePoolsPendingApproval = await _context.ServiceChargePools.CountAsync(pool => pool.Status == ServiceChargePoolStatus.ForApproval);
        ServiceChargePoolsPendingPosting = await _context.ServiceChargePools.CountAsync(pool => pool.Status == ServiceChargePoolStatus.Approved);

        var laborActuals = await _context.PayrollCostEntries
            .Where(entry => entry.DepartmentId != null &&
                entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate < nextMonth &&
                entry.PayrollPeriod.EndDate >= monthStart &&
                entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled)
            .GroupBy(entry => entry.DepartmentId)
            .Select(group => new { DepartmentId = group.Key, Cost = group.Sum(entry => entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay) })
            .ToListAsync();
        var laborBudgets = await _context.DepartmentLaborBudgets
            .Where(budget => budget.Month == today.Month && budget.Year == today.Year)
            .ToListAsync();
        DepartmentsOverLaborBudget = laborBudgets.Count(budget => laborActuals.Where(actual => actual.DepartmentId == budget.DepartmentId).Sum(actual => actual.Cost) > budget.BudgetedLaborCost);

        OpenGroupFolios = await _context.GroupFolios.CountAsync(folio => folio.Status == GroupFolioStatus.Open);
        GroupDepositsReceived = await _context.GroupDeposits
            .Where(deposit => deposit.Status != GroupDepositStatus.Cancelled)
            .SumAsync(deposit => (decimal?)deposit.Amount) ?? 0;
        GroupOutstandingBalances = await _context.GroupFolios
            .Where(groupFolio => groupFolio.FolioId != null && groupFolio.Status != GroupFolioStatus.Cancelled)
            .Select(groupFolio =>
                (_context.FolioItems
                    .Where(item => item.FolioId == groupFolio.FolioId && !item.IsVoided)
                    .Sum(item => (decimal?)item.Amount) ?? 0) -
                (_context.Payments
                    .Where(payment => payment.FolioId == groupFolio.FolioId && payment.Status != PaymentStatus.Voided && payment.Status != PaymentStatus.Failed)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0))
            .SumAsync();
    }
}
