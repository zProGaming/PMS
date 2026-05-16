using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class PaymentVoucherRegisterModel(ApplicationDbContext context) : PageModel
{
    public IList<PaymentVoucher> Vouchers { get; private set; } = [];
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate ?? DateTime.Today.AddDays(-30);
        EndDate = endDate ?? DateTime.Today;
        Vouchers = await context.PaymentVouchers.AsNoTracking()
            .Include(voucher => voucher.Supplier)
            .Include(voucher => voucher.APInvoice)
            .Where(voucher => voucher.VoucherDate >= StartDate && voucher.VoucherDate <= EndDate)
            .OrderBy(voucher => voucher.VoucherDate)
            .ToListAsync();
    }
}
