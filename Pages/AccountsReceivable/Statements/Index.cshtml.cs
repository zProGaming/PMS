using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.AccountsReceivable.Statements;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty(SupportsGet = true)]
    public int? ARAccountId { get; set; }

    public SelectList AccountOptions { get; set; } = null!;
    public ARAccount? Account { get; set; }
    public IList<ARInvoice> Invoices { get; set; } = new List<ARInvoice>();
    public IList<ARPayment> Payments { get; set; } = new List<ARPayment>();

    public async Task OnGetAsync()
    {
        var accounts = await _context.ARAccounts.AsNoTracking().OrderBy(account => account.AccountName).ToListAsync();
        AccountOptions = new SelectList(accounts, "Id", "AccountName", ARAccountId);

        if (ARAccountId is null)
        {
            return;
        }

        Account = await _context.ARAccounts.AsNoTracking().FirstOrDefaultAsync(account => account.Id == ARAccountId);
        Invoices = await _context.ARInvoices
            .AsNoTracking()
            .Where(invoice => invoice.ARAccountId == ARAccountId)
            .OrderBy(invoice => invoice.InvoiceDate)
            .ToListAsync();
        Payments = await _context.ARPayments
            .AsNoTracking()
            .Include(payment => payment.Allocations)
            .Where(payment => payment.ARAccountId == ARAccountId)
            .OrderBy(payment => payment.PaymentDate)
            .ToListAsync();
    }
}
