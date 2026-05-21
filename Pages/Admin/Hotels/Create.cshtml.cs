using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Hotels;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Hotel Hotel { get; set; } = default!;

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Hotel.Code = Hotel.Code.Trim().ToUpperInvariant();

        if (await _context.Hotels.AnyAsync(hotel => hotel.Code == Hotel.Code))
        {
            ModelState.AddModelError("Hotel.Code", "Company Code must be unique.");
            return Page();
        }

        _context.Hotels.Add(Hotel);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
