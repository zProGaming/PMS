using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RoomInventoryControls;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<RoomInventoryControl> RoomInventoryControls { get; set; } = new List<RoomInventoryControl>();

    public async Task OnGetAsync()
    {
        RoomInventoryControls = await _context.RoomInventoryControls
            .Include(r => r.RoomType)
            .OrderBy(r => r.InventoryDate)
            .ThenBy(r => r.RoomType != null ? r.RoomType.Code : string.Empty)
            .ToListAsync();
    }
}
