using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Leads;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<SalesLead> SalesLeads { get; set; } = new List<SalesLead>();

    public async Task OnGetAsync()
    {
        SalesLeads = await _context.SalesLeads
            .Include(lead => lead.SalesAccount)
            .AsNoTracking()
            .OrderByDescending(lead => lead.ExpectedCloseDate)
            .ThenBy(lead => lead.LeadName)
            .ToListAsync();
    }
}
