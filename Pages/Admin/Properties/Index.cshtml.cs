using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Properties;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<Property> Properties { get; set; } = new List<Property>();

    public async Task OnGetAsync()
    {
        Properties = await _context.Properties
            .Include(property => property.Hotel)
            .AsNoTracking()
            .OrderBy(property => property.Name)
            .ToListAsync();
    }
}
