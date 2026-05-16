using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.Events;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public BanquetEvent BanquetEvent { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var banquetEvent = await _context.BanquetEvents
            .Include(e => e.FunctionRoom)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (banquetEvent is null)
        {
            return NotFound();
        }

        BanquetEvent = banquetEvent;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var banquetEvent = await _context.BanquetEvents
            .Include(e => e.BanquetEventOrder)
            .Include(e => e.Charges)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (banquetEvent is not null)
        {
            _context.BanquetCharges.RemoveRange(banquetEvent.Charges);
            if (banquetEvent.BanquetEventOrder is not null)
            {
                _context.BanquetEventOrders.Remove(banquetEvent.BanquetEventOrder);
            }

            _context.BanquetEvents.Remove(banquetEvent);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
