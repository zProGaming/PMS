using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Pages.BookingManagement.AddOns;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<BookingAddOn> AddOns { get; set; } = new List<BookingAddOn>();

    public async Task OnGetAsync()
    {
        AddOns = await _context.BookingAddOns
            .AsNoTracking()
            .OrderBy(addOn => addOn.Name)
            .ToListAsync();
    }
}
