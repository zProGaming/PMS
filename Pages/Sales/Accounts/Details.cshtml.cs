using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Accounts;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public SalesAccount SalesAccount { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var account = await _context.SalesAccounts
            .Include(account => account.ContactPersons)
            .Include(account => account.SalesLeads)
            .Include(account => account.SalesActivities)
                .ThenInclude(activity => activity.SalesLead)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(account => account.Id == id);

        if (account is null)
        {
            return NotFound();
        }

        SalesAccount = account;
        return Page();
    }
}
