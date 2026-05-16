using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.AccountsPayable;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public decimal APBalance { get; private set; }
    public decimal APOverdue { get; private set; }
    public int PendingInvoices { get; private set; }
    public int PendingVouchers { get; private set; }
    public int VouchersForRelease { get; private set; }
    public int BankReconciliationsPending { get; private set; }
    public string OpenPeriodName { get; private set; } = "None";

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        APBalance = await context.APInvoices.AsNoTracking()
            .Where(invoice => invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
            .SumAsync(invoice => invoice.Balance);
        APOverdue = await context.APInvoices.AsNoTracking()
            .Where(invoice => invoice.DueDate < today && invoice.Balance > 0 && invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
            .SumAsync(invoice => invoice.Balance);
        PendingInvoices = await context.APInvoices.AsNoTracking().CountAsync(invoice => invoice.Status == APInvoiceStatus.ForApproval || invoice.Status == APInvoiceStatus.Draft);
        PendingVouchers = await context.PaymentVouchers.AsNoTracking().CountAsync(voucher => voucher.Status == PaymentVoucherStatus.ForApproval || voucher.Status == PaymentVoucherStatus.Draft);
        VouchersForRelease = await context.PaymentVouchers.AsNoTracking().CountAsync(voucher => voucher.Status == PaymentVoucherStatus.Approved);
        BankReconciliationsPending = await context.BankReconciliations.AsNoTracking().CountAsync(reconciliation => reconciliation.Status != BankReconciliationStatus.Approved && reconciliation.Status != BankReconciliationStatus.Cancelled);
        OpenPeriodName = await context.AccountingPeriods.AsNoTracking()
            .Where(period => period.Status == AccountingPeriodStatus.Open && period.StartDate <= today && period.EndDate >= today)
            .Select(period => period.PeriodName)
            .FirstOrDefaultAsync() ?? "None";
    }
}
