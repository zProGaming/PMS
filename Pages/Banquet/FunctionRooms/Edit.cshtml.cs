using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.FunctionRooms;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public FunctionRoom FunctionRoom { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var room = await _context.FunctionRooms.FindAsync(id);
        if (room is null)
        {
            return NotFound();
        }

        FunctionRoom = room;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateFunctionRoom();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Attach(FunctionRoom).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private void ValidateFunctionRoom()
    {
        if (FunctionRoom.Capacity < 0)
        {
            ModelState.AddModelError("FunctionRoom.Capacity", "Capacity cannot be negative.");
        }

        if (FunctionRoom.Rate < 0)
        {
            ModelState.AddModelError("FunctionRoom.Rate", "Rate cannot be negative.");
        }
    }
}
