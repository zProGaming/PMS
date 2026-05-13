using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Hotels;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public Hotel Hotel { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var hotel = await _context.Hotels
            .AsNoTracking()
            .FirstOrDefaultAsync(hotel => hotel.Id == id);

        if (hotel is null)
        {
            return NotFound();
        }

        Hotel = hotel;
        return Page();
    }
}
