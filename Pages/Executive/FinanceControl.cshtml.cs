using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.Executive;

public class FinanceControlModel(ApplicationDbContext context) : PageModel
{
    public int OpenCashierShifts { get; private set; }
    public int PendingVoids { get; private set; }
    public int PendingRefunds { get; private set; }
    public int PendingDiscounts { get; private set; }
    public int HighBalanceFolios { get; private set; }
    public decimal OverdueArBalance { get; private set; }
    public decimal OverdueApBalance { get; private set; }
    public int UnpostedTransactions { get; private set; }
    public string TrialBalanceStatus { get; private set; } = "Balanced";
    public string AccountingPeriodStatus { get; private set; } = "None";
    public int PendingBankReconciliations { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        OpenCashierShifts = await context.CashierShifts.AsNoTracking().CountAsync(shift => shift.Status == CashierShiftStatus.Open);
        PendingVoids = await context.VoidRequests.AsNoTracking().CountAsync(request => request.Status == ApprovalStatus.Pending);
        PendingRefunds = await context.RefundTransactions.AsNoTracking().CountAsync(refund => refund.Status == RefundStatus.Requested || refund.Status == RefundStatus.ForApproval);
        PendingDiscounts = await context.DiscountApprovals.AsNoTracking().CountAsync(discount => discount.Status == ApprovalStatus.Pending);
        OverdueArBalance = await context.ARInvoices.AsNoTracking().Where(invoice => invoice.DueDate < today && invoice.Balance > 0 && invoice.Status != ARInvoiceStatus.Cancelled && invoice.Status != ARInvoiceStatus.WrittenOff).SumAsync(invoice => (decimal?)invoice.Balance) ?? 0;
        OverdueApBalance = await context.APInvoices.AsNoTracking().Where(invoice => invoice.DueDate < today && invoice.Balance > 0 && invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided).SumAsync(invoice => (decimal?)invoice.Balance) ?? 0;
        UnpostedTransactions = await context.FolioItems.CountAsync(item => !item.IsVoided && !context.JournalEntries.Any(entry => entry.Status == JournalEntryStatus.Posted && entry.SourceReferenceId == item.Id && (entry.SourceTransactionType == SourceTransactionType.FolioCharge || entry.SourceTransactionType == SourceTransactionType.RoomCharge)));
        var tb = await context.JournalEntryLines.Where(line => line.JournalEntry != null && line.JournalEntry.Status == JournalEntryStatus.Posted).GroupBy(_ => 1).Select(group => new { Debit = group.Sum(line => line.DebitAmount), Credit = group.Sum(line => line.CreditAmount) }).FirstOrDefaultAsync();
        TrialBalanceStatus = (tb?.Debit ?? 0) == (tb?.Credit ?? 0) ? "Balanced" : "Out of balance";
        AccountingPeriodStatus = await context.AccountingPeriods.AsNoTracking().Where(period => period.StartDate <= today && period.EndDate >= today).Select(period => period.Status.ToString()).FirstOrDefaultAsync() ?? "None";
        PendingBankReconciliations = await context.BankReconciliations.AsNoTracking().CountAsync(recon => recon.Status != BankReconciliationStatus.Approved && recon.Status != BankReconciliationStatus.Cancelled);

        var balances = await context.Folios.AsNoTracking().Select(folio => new
        {
            Balance = (context.FolioItems.Where(item => item.FolioId == folio.Id && !item.IsVoided).Sum(item => (decimal?)item.Amount) ?? 0) -
                (context.Payments.Where(payment => payment.FolioId == folio.Id && payment.Status == PaymentStatus.Completed).Sum(payment => (decimal?)payment.Amount) ?? 0)
        }).ToListAsync();
        HighBalanceFolios = balances.Count(folio => folio.Balance >= 50000);
    }
}
