using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Purchasing.Receiving;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<ReceivingRecord> ReceivingRecords { get; set; } = new List<ReceivingRecord>();

    public async Task OnGetAsync()
    {
        ReceivingRecords = await _context.ReceivingRecords
            .AsNoTracking()
            .Include(record => record.PurchaseOrder)
            .Include(record => record.Supplier)
            .Include(record => record.Items)
            .OrderByDescending(record => record.ReceivedDate)
            .ThenByDescending(record => record.Id)
            .ToListAsync();
    }
}
