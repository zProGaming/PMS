using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class APAgingModel(ApplicationDbContext context) : PageModel
{
    public IList<AgingRow> Rows { get; private set; } = [];
    public DateTime AsOfDate { get; private set; }

    public async Task OnGetAsync(DateTime? asOfDate)
    {
        AsOfDate = (asOfDate ?? DateTime.Today).Date;
        var invoices = await context.APInvoices.AsNoTracking()
            .Include(invoice => invoice.Supplier)
            .Where(invoice => invoice.Balance > 0 && invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
            .ToListAsync();

        Rows = invoices
            .GroupBy(invoice => invoice.Supplier?.SupplierName ?? "Unknown Supplier")
            .Select(group =>
            {
                var row = new AgingRow { SupplierName = group.Key };
                foreach (var invoice in group)
                {
                    var days = Math.Max(0, (AsOfDate - invoice.DueDate.Date).Days);
                    if (days == 0) row.Current += invoice.Balance;
                    else if (days <= 30) row.Days1To30 += invoice.Balance;
                    else if (days <= 60) row.Days31To60 += invoice.Balance;
                    else if (days <= 90) row.Days61To90 += invoice.Balance;
                    else row.Over90 += invoice.Balance;
                }
                return row;
            })
            .OrderBy(row => row.SupplierName)
            .ToList();
    }

    public class AgingRow
    {
        public string SupplierName { get; set; } = string.Empty;
        public decimal Current { get; set; }
        public decimal Days1To30 { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Over90 { get; set; }
        public decimal Total => Current + Days1To30 + Days31To60 + Days61To90 + Over90;
    }
}
