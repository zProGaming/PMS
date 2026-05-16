using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.AccountsPayable.Disbursements;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<Disbursement> Disbursements { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Disbursements = await context.Disbursements.AsNoTracking()
            .Include(disbursement => disbursement.Supplier)
            .Include(disbursement => disbursement.PaymentVoucher)
            .OrderByDescending(disbursement => disbursement.DisbursementDate)
            .ThenByDescending(disbursement => disbursement.Id)
            .Take(250)
            .ToListAsync();
    }
}
