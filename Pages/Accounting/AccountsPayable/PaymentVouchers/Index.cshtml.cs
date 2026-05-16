using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.AccountsPayable.PaymentVouchers;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<PaymentVoucher> Vouchers { get; private set; } = [];
    public PaymentVoucherStatus? Status { get; private set; }

    public async Task OnGetAsync(PaymentVoucherStatus? status)
    {
        Status = status;
        var query = context.PaymentVouchers.AsNoTracking()
            .Include(voucher => voucher.Supplier)
            .Include(voucher => voucher.APInvoice)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(voucher => voucher.Status == status);
        }

        Vouchers = await query.OrderByDescending(voucher => voucher.VoucherDate).ThenByDescending(voucher => voucher.Id).Take(250).ToListAsync();
    }
}
