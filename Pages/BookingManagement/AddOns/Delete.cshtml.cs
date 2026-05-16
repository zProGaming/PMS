using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement.AddOns;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public BookingAddOn AddOn { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var addOn = await _context.BookingAddOns.FindAsync(id);
        if (addOn is null)
        {
            return NotFound();
        }

        AddOn = addOn;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var addOn = await _context.BookingAddOns.FindAsync(id);
        if (addOn is not null)
        {
            _context.BookingAddOns.Remove(addOn);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
