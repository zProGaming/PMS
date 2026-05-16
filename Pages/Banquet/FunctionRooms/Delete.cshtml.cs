using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.FunctionRooms;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public FunctionRoom FunctionRoom { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var room = await _context.FunctionRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(room => room.Id == id);

        if (room is null)
        {
            return NotFound();
        }

        FunctionRoom = room;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var room = await _context.FunctionRooms.FindAsync(id);
        if (room is not null)
        {
            _context.FunctionRooms.Remove(room);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
