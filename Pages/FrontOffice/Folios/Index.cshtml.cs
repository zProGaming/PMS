using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.FrontOffice.Folios;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<Folio> Folios { get; private set; } = [];

    public int OpenFolios { get; private set; }

    public decimal TotalOpenBalance { get; private set; }

    public int HighBalanceFolios { get; private set; }

    public async Task OnGetAsync()
    {
        Folios = await context.Folios
            .Include(folio => folio.Guest)
            .Include(folio => folio.Reservation).ThenInclude(reservation => reservation!.Room)
            .Include(folio => folio.Items)
            .Include(folio => folio.Payments)
            .AsNoTracking()
            .OrderByDescending(folio => folio.OpenedAtUtc)
            .Take(200)
            .ToListAsync();

        OpenFolios = Folios.Count(folio => folio.Status == FolioStatus.Open);
        TotalOpenBalance = Folios
            .Where(folio => folio.Status == FolioStatus.Open)
            .Sum(folio => folio.Balance);
        HighBalanceFolios = Folios.Count(folio => folio.Balance >= 10000m);
    }
}
