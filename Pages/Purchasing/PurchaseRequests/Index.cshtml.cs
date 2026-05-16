using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Purchasing.PurchaseRequests;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<PurchaseRequest> PurchaseRequests { get; set; } = new List<PurchaseRequest>();

    public async Task OnGetAsync()
    {
        PurchaseRequests = await _context.PurchaseRequests
            .AsNoTracking()
            .Include(request => request.Department)
            .Include(request => request.Items)
            .OrderByDescending(request => request.RequestDate)
            .ThenByDescending(request => request.Id)
            .ToListAsync();
    }
}
