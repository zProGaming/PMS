using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Accounts;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<SalesAccount> SalesAccounts { get; set; } = new List<SalesAccount>();

    public async Task OnGetAsync()
    {
        SalesAccounts = await _context.SalesAccounts
            .Include(account => account.ContactPersons)
            .AsNoTracking()
            .OrderBy(account => account.AccountName)
            .ToListAsync();
    }
}
