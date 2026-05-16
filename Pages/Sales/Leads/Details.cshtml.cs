using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Leads;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public SalesLead SalesLead { get; set; } = default!;

    public DateTime? NextFollowUpDate { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var lead = await _context.SalesLeads
            .Include(lead => lead.SalesAccount)
            .Include(lead => lead.SalesActivities)
                .ThenInclude(activity => activity.SalesAccount)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(lead => lead.Id == id);

        if (lead is null)
        {
            return NotFound();
        }

        SalesLead = lead;
        NextFollowUpDate = lead.SalesActivities
            .Where(activity => activity.NextFollowUpDate is not null)
            .OrderBy(activity => activity.NextFollowUpDate)
            .Select(activity => activity.NextFollowUpDate)
            .FirstOrDefault();

        return Page();
    }
}
