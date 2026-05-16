using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.ContactPersons;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public int? AccountId { get; set; }

    public IList<ContactPerson> ContactPersons { get; set; } = new List<ContactPerson>();

    public async Task OnGetAsync(int? accountId)
    {
        AccountId = accountId;

        var query = _context.ContactPersons
            .Include(contact => contact.SalesAccount)
            .AsNoTracking();

        if (accountId is not null)
        {
            query = query.Where(contact => contact.SalesAccountId == accountId);
        }

        ContactPersons = await query
            .OrderBy(contact => contact.SalesAccount!.AccountName)
            .ThenByDescending(contact => contact.IsPrimary)
            .ThenBy(contact => contact.FullName)
            .ToListAsync();
    }
}
