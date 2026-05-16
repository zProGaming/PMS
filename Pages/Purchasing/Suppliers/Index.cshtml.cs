using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Purchasing.Suppliers;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<Supplier> Suppliers { get; set; } = new List<Supplier>();

    public async Task OnGetAsync()
    {
        Suppliers = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.SupplierName)
            .ToListAsync();
    }
}
