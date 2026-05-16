using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.AccountsReceivable.PaymentAllocations;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<ARPaymentAllocation> Allocations { get; set; } = new List<ARPaymentAllocation>();

    public async Task OnGetAsync()
    {
        Allocations = await _context.ARPaymentAllocations
            .AsNoTracking()
            .Include(allocation => allocation.ARPayment)
                .ThenInclude(payment => payment!.ARAccount)
            .Include(allocation => allocation.ARInvoice)
            .OrderByDescending(allocation => allocation.AllocationDate)
            .Take(300)
            .ToListAsync();
    }
}
