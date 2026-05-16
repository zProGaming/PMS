using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.AccountsReceivable.Aging;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<ARAgingRow> Rows { get; set; } = new List<ARAgingRow>();

    public decimal CurrentTotal => Rows.Sum(row => row.Current);
    public decimal Days1To30Total => Rows.Sum(row => row.Days1To30);
    public decimal Days31To60Total => Rows.Sum(row => row.Days31To60);
    public decimal Days61To90Total => Rows.Sum(row => row.Days61To90);
    public decimal Over90Total => Rows.Sum(row => row.Over90);
    public decimal GrandTotal => Rows.Sum(row => row.Total);

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var invoices = await _context.ARInvoices
            .AsNoTracking()
            .Include(invoice => invoice.ARAccount)
            .Where(invoice => invoice.Balance > 0 &&
                invoice.Status != ARInvoiceStatus.Cancelled &&
                invoice.Status != ARInvoiceStatus.WrittenOff)
            .ToListAsync();

        Rows = invoices
            .GroupBy(invoice => new { invoice.ARAccountId, invoice.ARAccount!.AccountName })
            .Select(group =>
            {
                var row = new ARAgingRow(group.Key.AccountName);
                foreach (var invoice in group)
                {
                    var age = (today - invoice.DueDate.Date).Days;
                    if (age <= 0)
                    {
                        row.Current += invoice.Balance;
                    }
                    else if (age <= 30)
                    {
                        row.Days1To30 += invoice.Balance;
                    }
                    else if (age <= 60)
                    {
                        row.Days31To60 += invoice.Balance;
                    }
                    else if (age <= 90)
                    {
                        row.Days61To90 += invoice.Balance;
                    }
                    else
                    {
                        row.Over90 += invoice.Balance;
                    }
                }

                return row;
            })
            .OrderBy(row => row.AccountName)
            .ToList();
    }

    public class ARAgingRow(string accountName)
    {
        public string AccountName { get; set; } = accountName;
        public decimal Current { get; set; }
        public decimal Days1To30 { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Over90 { get; set; }
        public decimal Total => Current + Days1To30 + Days31To60 + Days61To90 + Over90;
    }
}
