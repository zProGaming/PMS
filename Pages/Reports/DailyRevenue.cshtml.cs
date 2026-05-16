using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.Reports;

[Authorize(Policy = PmsPolicies.FinanceApprovals)]
public class DailyRevenueModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public DateTime BusinessDate { get; set; }

    public decimal RoomCharges { get; set; }

    public decimal OtherCharges { get; set; }

    public decimal Payments { get; set; }

    public decimal OutstandingBalances { get; set; }

    public async Task OnGetAsync()
    {
        BusinessDate = await GetBusinessDateAsync();
        var nextBusinessDate = BusinessDate.AddDays(1);

        var charges = await _context.FolioItems
            .AsNoTracking()
            .Where(item =>
                !item.IsVoided &&
                item.PostingDate >= BusinessDate &&
                item.PostingDate < nextBusinessDate)
            .ToListAsync();

        RoomCharges = charges
            .Where(IsRoomCharge)
            .Sum(item => item.Amount);

        OtherCharges = charges
            .Where(item => !IsRoomCharge(item))
            .Sum(item => item.Amount);

        Payments = await _context.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.Status == PaymentStatus.Completed &&
                payment.PaymentDate >= BusinessDate &&
                payment.PaymentDate < nextBusinessDate)
            .SumAsync(payment => payment.Amount);

        var openFolios = await _context.Folios
            .Include(folio => folio.Items)
            .Include(folio => folio.Payments)
            .AsNoTracking()
            .AsSplitQuery()
            .Where(folio => folio.Status == FolioStatus.Open)
            .ToListAsync();

        OutstandingBalances = openFolios
            .Select(folio => folio.Balance)
            .Where(balance => balance > 0)
            .Sum();
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }

    private static bool IsRoomCharge(FolioItem item)
    {
        return item.ChargeCode.StartsWith("ROOM", StringComparison.OrdinalIgnoreCase);
    }
}
