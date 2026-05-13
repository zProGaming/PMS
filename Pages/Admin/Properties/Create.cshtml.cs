using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Properties;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Property Property { get; set; } = default!;

    public SelectList HotelOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadHotelOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadHotelOptionsAsync(Property.HotelId);
            return Page();
        }

        _context.Properties.Add(Property);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task LoadHotelOptionsAsync(object? selectedHotel = null)
    {
        var hotels = await _context.Hotels
            .AsNoTracking()
            .OrderBy(hotel => hotel.Name)
            .Select(hotel => new { hotel.Id, hotel.Name })
            .ToListAsync();

        HotelOptions = new SelectList(hotels, "Id", "Name", selectedHotel);
    }
}
