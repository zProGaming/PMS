using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.Finance.Reports;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<CashierShift> CashierShifts { get; set; } = new List<CashierShift>();
    public IList<CashierTransaction> CashierTransactions { get; set; } = new List<CashierTransaction>();
    public IList<Payment> Payments { get; set; } = new List<Payment>();
    public IList<RefundTransaction> Refunds { get; set; } = new List<RefundTransaction>();
    public IList<VoidRequest> VoidRequests { get; set; } = new List<VoidRequest>();
    public IList<DiscountApproval> DiscountApprovals { get; set; } = new List<DiscountApproval>();
    public IList<CreditMemo> CreditMemos { get; set; } = new List<CreditMemo>();
    public IList<DebitMemo> DebitMemos { get; set; } = new List<DebitMemo>();

    public async Task OnGetAsync()
    {
        CashierShifts = await _context.CashierShifts.AsNoTracking().OrderByDescending(item => item.OpenedAt).Take(50).ToListAsync();
        CashierTransactions = await _context.CashierTransactions.AsNoTracking().Include(item => item.CashierShift).OrderByDescending(item => item.TransactionDate).Take(100).ToListAsync();
        Payments = await _context.Payments.AsNoTracking().Include(item => item.Folio).OrderByDescending(item => item.PaymentDate).Take(100).ToListAsync();
        Refunds = await _context.RefundTransactions.AsNoTracking().OrderByDescending(item => item.RefundDate).Take(100).ToListAsync();
        VoidRequests = await _context.VoidRequests.AsNoTracking().OrderByDescending(item => item.RequestedAt).Take(100).ToListAsync();
        DiscountApprovals = await _context.DiscountApprovals.AsNoTracking().OrderByDescending(item => item.RequestedAt).Take(100).ToListAsync();
        CreditMemos = await _context.CreditMemos.AsNoTracking().Include(item => item.ARAccount).OrderByDescending(item => item.CreditMemoDate).Take(100).ToListAsync();
        DebitMemos = await _context.DebitMemos.AsNoTracking().Include(item => item.ARAccount).OrderByDescending(item => item.DebitMemoDate).Take(100).ToListAsync();
    }
}
