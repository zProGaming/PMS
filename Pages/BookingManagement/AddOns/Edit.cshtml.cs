using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement.AddOns;

public class EditModel(ApplicationDbContext context) : PageModel
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

    public async Task<IActionResult> OnPostAsync()
    {
        if (AddOn.Price < 0)
        {
            ModelState.AddModelError("AddOn.Price", "Price cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Attach(AddOn).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
