using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Admin.RoomTypes;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public RoomType RoomType { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var roomType = await _context.RoomTypes
            .Include(roomType => roomType.Property)
            .AsNoTracking()
            .FirstOrDefaultAsync(roomType => roomType.Id == id);

        if (roomType is null)
        {
            return NotFound();
        }

        RoomType = roomType;
        return Page();
    }
}
