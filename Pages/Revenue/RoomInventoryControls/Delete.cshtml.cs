using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RoomInventoryControls;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public RoomInventoryControl RoomInventoryControl { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var roomInventoryControl = await _context.RoomInventoryControls
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (roomInventoryControl == null)
        {
            return NotFound();
        }

        RoomInventoryControl = roomInventoryControl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var roomInventoryControl = await _context.RoomInventoryControls.FindAsync(id);
        if (roomInventoryControl != null)
        {
            _context.RoomInventoryControls.Remove(roomInventoryControl);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
