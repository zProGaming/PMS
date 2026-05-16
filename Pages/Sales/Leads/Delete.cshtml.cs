using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Leads;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public SalesLead SalesLead { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var lead = await _context.SalesLeads
            .Include(lead => lead.SalesAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(lead => lead.Id == id);

        if (lead is null)
        {
            return NotFound();
        }

        SalesLead = lead;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var lead = await _context.SalesLeads.FindAsync(id);
        if (lead is not null)
        {
            _context.SalesLeads.Remove(lead);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
