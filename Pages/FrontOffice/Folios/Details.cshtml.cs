using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.FrontOffice.Folios;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public Folio Folio { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var folio = await _context.Folios
            .Include(folio => folio.Guest)
            .Include(folio => folio.Reservation)
                .ThenInclude(reservation => reservation!.Room)
            .Include(folio => folio.Items)
                .ThenInclude(item => item.ChargeCodeDefinition)
            .Include(folio => folio.Payments)
                .ThenInclude(payment => payment.CashierTransactions)
                    .ThenInclude(transaction => transaction.CashierShift)
            .AsNoTracking()
            .FirstOrDefaultAsync(folio => folio.Id == id);

        if (folio is null)
        {
            return NotFound();
        }

        Folio = folio;
        return Page();
    }
}
