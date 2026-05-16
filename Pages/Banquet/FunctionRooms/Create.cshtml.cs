using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.FunctionRooms;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public FunctionRoom FunctionRoom { get; set; } = new() { IsActive = true };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateFunctionRoom();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.FunctionRooms.Add(FunctionRoom);
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
