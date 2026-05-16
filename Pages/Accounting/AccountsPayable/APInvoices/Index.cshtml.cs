using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.AccountsPayable.APInvoices;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<APInvoice> Invoices { get; private set; } = [];
    public APInvoiceStatus? Status { get; private set; }
    public string? Search { get; private set; }

    public async Task OnGetAsync(APInvoiceStatus? status, string? search)
    {
        Status = status;
        Search = search;
        var query = context.APInvoices.AsNoTracking()
            .Include(invoice => invoice.Supplier)
            .Include(invoice => invoice.PurchaseOrder)
            .Include(invoice => invoice.ReceivingRecord)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(invoice => invoice.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(invoice => invoice.InvoiceNumber.Contains(search) || (invoice.Supplier != null && invoice.Supplier.SupplierName.Contains(search)));
        }

        Invoices = await query.OrderByDescending(invoice => invoice.InvoiceDate).ThenByDescending(invoice => invoice.Id).Take(250).ToListAsync();
    }
}
