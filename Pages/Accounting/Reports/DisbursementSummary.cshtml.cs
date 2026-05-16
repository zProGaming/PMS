using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class DisbursementSummaryModel(ApplicationDbContext context) : PageModel
{
    public IList<Disbursement> Disbursements { get; private set; } = [];
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate ?? DateTime.Today.AddDays(-30);
        EndDate = endDate ?? DateTime.Today;
        Disbursements = await context.Disbursements.AsNoTracking()
            .Include(item => item.Supplier)
            .Include(item => item.PaymentVoucher)
            .Where(item => item.DisbursementDate >= StartDate && item.DisbursementDate <= EndDate)
            .OrderBy(item => item.DisbursementDate)
            .ToListAsync();
    }
}
