using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public int EventsToday { get; set; }

    public int UpcomingEvents { get; set; }

    public int TentativeEvents { get; set; }

    public int ConfirmedEvents { get; set; }

    public int CompletedEventsThisMonth { get; set; }

    public int CancelledEventsThisMonth { get; set; }

    public decimal TotalExpectedBanquetRevenueThisMonth { get; set; }

    public IList<BanquetEvent> UpcomingEventList { get; set; } = new List<BanquetEvent>();

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        EventsToday = await _context.BanquetEvents.CountAsync(banquetEvent =>
            banquetEvent.EventDate >= today &&
            banquetEvent.EventDate < tomorrow &&
            banquetEvent.EventStatus != BanquetEventStatus.Cancelled &&
            banquetEvent.EventStatus != BanquetEventStatus.Lost);

        UpcomingEvents = await _context.BanquetEvents.CountAsync(banquetEvent =>
            banquetEvent.EventDate >= today &&
            banquetEvent.EventStatus != BanquetEventStatus.Cancelled &&
            banquetEvent.EventStatus != BanquetEventStatus.Lost);

        TentativeEvents = await _context.BanquetEvents.CountAsync(banquetEvent =>
            banquetEvent.EventStatus == BanquetEventStatus.Tentative);

        ConfirmedEvents = await _context.BanquetEvents.CountAsync(banquetEvent =>
            banquetEvent.EventStatus == BanquetEventStatus.Confirmed);

        CompletedEventsThisMonth = await _context.BanquetEvents.CountAsync(banquetEvent =>
            banquetEvent.EventDate >= monthStart &&
            banquetEvent.EventDate < nextMonth &&
            banquetEvent.EventStatus == BanquetEventStatus.Completed);

        CancelledEventsThisMonth = await _context.BanquetEvents.CountAsync(banquetEvent =>
            banquetEvent.EventDate >= monthStart &&
            banquetEvent.EventDate < nextMonth &&
            banquetEvent.EventStatus == BanquetEventStatus.Cancelled);

        var monthEvents = await _context.BanquetEvents
            .Include(banquetEvent => banquetEvent.FunctionRoom)
            .Include(banquetEvent => banquetEvent.BanquetPackage)
            .Include(banquetEvent => banquetEvent.Charges)
            .AsNoTracking()
            .AsSplitQuery()
            .Where(banquetEvent =>
                banquetEvent.EventDate >= monthStart &&
                banquetEvent.EventDate < nextMonth &&
                banquetEvent.EventStatus != BanquetEventStatus.Cancelled &&
                banquetEvent.EventStatus != BanquetEventStatus.Lost)
            .ToListAsync();

        TotalExpectedBanquetRevenueThisMonth = monthEvents.Sum(banquetEvent =>
            (banquetEvent.FunctionRoom?.Rate ?? 0) +
            ((banquetEvent.BanquetPackage?.PricePerPax ?? 0) * Math.Max(banquetEvent.GuaranteedPax, banquetEvent.BanquetPackage?.MinimumPax ?? 0)) +
            banquetEvent.Charges.Where(charge => !charge.IsVoided).Sum(charge => charge.Amount));

        UpcomingEventList = await _context.BanquetEvents
            .Include(banquetEvent => banquetEvent.FunctionRoom)
            .AsNoTracking()
            .Where(banquetEvent =>
                banquetEvent.EventDate >= today &&
                banquetEvent.EventStatus != BanquetEventStatus.Cancelled &&
                banquetEvent.EventStatus != BanquetEventStatus.Lost)
            .OrderBy(banquetEvent => banquetEvent.EventDate)
            .ThenBy(banquetEvent => banquetEvent.StartTime)
            .Take(10)
            .ToListAsync();
    }
}
