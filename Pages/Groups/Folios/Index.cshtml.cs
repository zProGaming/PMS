using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Groups;

namespace Vantage.PMS.Pages.Groups.Folios;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<GroupFolio> GroupFolios { get; private set; } = [];

    public async Task OnGetAsync()
    {
        GroupFolios = await context.GroupFolios
            .Include(item => item.GroupBooking)
            .Include(item => item.PseudoRoom)
            .Include(item => item.Folio).ThenInclude(item => item!.Items)
            .Include(item => item.Folio).ThenInclude(item => item!.Payments)
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();
    }
}
