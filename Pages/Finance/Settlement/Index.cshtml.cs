using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Finance.Settlement;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int? FolioId { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int Total { get; private set; }
    public const int PageSize = 25;
    public IList<SettlementRow> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var query = context.Folios.AsNoTracking().Where(f => f.Status == FolioStatus.Open)
            .Select(f => new SettlementRow
            {
                Id = f.Id, FolioNumber = f.FolioNumber, GuestName = f.Guest!.FirstName + " " + f.Guest.LastName,
                ReservationNumber = f.Reservation!.ConfirmationNumber, Departed = f.Reservation.Status == ReservationStatus.CheckedOut,
                Balance = (f.Items.Where(i => !i.IsVoided).Sum(i => (decimal?)i.Amount) ?? 0) -
                    (f.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => (decimal?)p.Amount) ?? 0)
            }).Where(f => f.Balance != 0);
        if (FolioId.HasValue) query = query.Where(f => f.Id == FolioId);
        if (!string.IsNullOrWhiteSpace(Search))
        {
            Search = Search.Trim();
            query = query.Where(f => f.FolioNumber.Contains(Search) || f.GuestName.Contains(Search) || f.ReservationNumber.Contains(Search));
        }
        Total = await query.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)));
        Rows = await query.OrderByDescending(f => f.Departed).ThenBy(f => f.Id).Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
    }

    public class SettlementRow
    {
        public int Id { get; set; }
        public string FolioNumber { get; set; } = "";
        public string GuestName { get; set; } = "";
        public string ReservationNumber { get; set; } = "";
        public bool Departed { get; set; }
        public decimal Balance { get; set; }
    }
}
