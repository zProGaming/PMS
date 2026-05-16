using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.FunctionRooms;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<FunctionRoom> FunctionRooms { get; set; } = new List<FunctionRoom>();

    public async Task OnGetAsync()
    {
        FunctionRooms = await _context.FunctionRooms
            .AsNoTracking()
            .OrderBy(room => room.Name)
            .ToListAsync();
    }
}
