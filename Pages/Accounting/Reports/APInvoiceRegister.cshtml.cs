using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class APInvoiceRegisterModel(ApplicationDbContext context) : PageModel
{
    public IList<APInvoice> Invoices { get; private set; } = [];
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate ?? DateTime.Today.AddDays(-30);
        EndDate = endDate ?? DateTime.Today;
        Invoices = await context.APInvoices.AsNoTracking()
            .Include(invoice => invoice.Supplier)
            .Where(invoice => invoice.InvoiceDate >= StartDate && invoice.InvoiceDate <= EndDate)
            .OrderBy(invoice => invoice.InvoiceDate)
            .ToListAsync();
    }
}
