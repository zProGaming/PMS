using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Admin.RoomTypes;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<RoomType> RoomTypes { get; set; } = new List<RoomType>();

    public async Task OnGetAsync()
    {
        RoomTypes = await _context.RoomTypes
            .Include(roomType => roomType.Property)
            .AsNoTracking()
            .OrderBy(roomType => roomType.Name)
            .ToListAsync();
    }
}
