using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class SupplierLedgerModel(ApplicationDbContext context) : PageModel
{
    public IList<LedgerRow> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var invoices = await context.APInvoices.AsNoTracking().Include(invoice => invoice.Supplier).ToListAsync();
        var vouchers = await context.PaymentVouchers.AsNoTracking().Include(voucher => voucher.Supplier).Where(voucher => voucher.Status == PaymentVoucherStatus.Released).ToListAsync();

        Rows = invoices.Select(invoice => new LedgerRow
            {
                Supplier = invoice.Supplier?.SupplierName ?? "Unknown Supplier",
                Date = invoice.InvoiceDate,
                Reference = invoice.InvoiceNumber,
                Type = "AP Invoice",
                Debit = 0,
                Credit = invoice.TotalAmount,
                BalanceEffect = invoice.Balance
            })
            .Concat(vouchers.Select(voucher => new LedgerRow
            {
                Supplier = voucher.Supplier?.SupplierName ?? "Unknown Supplier",
                Date = voucher.VoucherDate,
                Reference = voucher.VoucherNumber,
                Type = "Payment Voucher",
                Debit = voucher.Amount,
                Credit = 0,
                BalanceEffect = -voucher.Amount
            }))
            .OrderBy(row => row.Supplier)
            .ThenBy(row => row.Date)
            .ToList();
    }

    public class LedgerRow
    {
        public string Supplier { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal BalanceEffect { get; set; }
    }
}
