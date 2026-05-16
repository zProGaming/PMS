using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement.AddOns;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public BookingAddOn AddOn { get; set; } = new() { IsActive = true };

    public IActionResult OnGet()
    {
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

        _context.BookingAddOns.Add(AddOn);
        await _context.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
