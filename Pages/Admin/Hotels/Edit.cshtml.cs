using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Hotels;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Hotel Hotel { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var hotel = await _context.Hotels.FindAsync(id);
        if (hotel is null)
        {
            return NotFound();
        }

        Hotel = hotel;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Hotel.Code = Hotel.Code.Trim().ToUpperInvariant();

        if (await _context.Hotels.AnyAsync(hotel => hotel.Id != Hotel.Id && hotel.Code == Hotel.Code))
        {
            ModelState.AddModelError("Hotel.Code", "Company Code must be unique.");
            return Page();
        }

        _context.Attach(Hotel).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await HotelExistsAsync(Hotel.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToPage("./Index");
    }

    private Task<bool> HotelExistsAsync(int id)
    {
        return _context.Hotels.AnyAsync(hotel => hotel.Id == id);
    }
}
