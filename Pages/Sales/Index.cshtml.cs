using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public int TotalActiveAccounts { get; set; }

    public int OpenLeads { get; set; }

    public int LeadsWonThisMonth { get; set; }

    public int LeadsLostThisMonth { get; set; }

    public int UpcomingFollowUps { get; set; }

    public int OverdueFollowUps { get; set; }

    public decimal EstimatedPipelineValue { get; set; }
    public int ConfirmedGroupsThisMonth { get; set; }
    public int GroupRoomsBlockedThisMonth { get; set; }
    public decimal GroupDepositPipeline { get; set; }

    public IList<SalesLead> RecentOpenLeads { get; set; } = new List<SalesLead>();

    public IList<SalesActivity> FollowUps { get; set; } = new List<SalesActivity>();

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var followUpEnd = today.AddDays(14);
        var openStatuses = new[]
        {
            SalesLeadStatus.New,
            SalesLeadStatus.Contacted,
            SalesLeadStatus.ProposalSent,
            SalesLeadStatus.Negotiation
        };

        TotalActiveAccounts = await _context.SalesAccounts.CountAsync(account => account.IsActive);
        OpenLeads = await _context.SalesLeads.CountAsync(lead => openStatuses.Contains(lead.Status));
        LeadsWonThisMonth = await _context.SalesLeads.CountAsync(lead =>
            lead.Status == SalesLeadStatus.Won &&
            lead.ExpectedCloseDate >= monthStart &&
            lead.ExpectedCloseDate < nextMonth);
        LeadsLostThisMonth = await _context.SalesLeads.CountAsync(lead =>
            lead.Status == SalesLeadStatus.Lost &&
            lead.ExpectedCloseDate >= monthStart &&
            lead.ExpectedCloseDate < nextMonth);
        UpcomingFollowUps = await _context.SalesActivities.CountAsync(activity =>
            activity.NextFollowUpDate >= today &&
            activity.NextFollowUpDate < followUpEnd);
        OverdueFollowUps = await _context.SalesActivities.CountAsync(activity =>
            activity.NextFollowUpDate < today);
        EstimatedPipelineValue = await _context.SalesLeads
            .Where(lead => openStatuses.Contains(lead.Status))
            .SumAsync(lead => lead.EstimatedValue);
        ConfirmedGroupsThisMonth = await _context.GroupBookings.CountAsync(group =>
            group.BookingStatus == GroupBookingStatus.Confirmed &&
            group.ArrivalDate >= monthStart &&
            group.ArrivalDate < nextMonth);
        GroupRoomsBlockedThisMonth = await _context.GroupRoomBlocks
            .Where(block => block.BlockDate >= monthStart && block.BlockDate < nextMonth && block.GroupBooking != null && block.GroupBooking.BookingStatus != GroupBookingStatus.Cancelled)
            .SumAsync(block => (int?)block.RoomsBlocked) ?? 0;
        GroupDepositPipeline = await _context.GroupBookings
            .Where(group => group.BookingStatus == GroupBookingStatus.Confirmed || group.BookingStatus == GroupBookingStatus.Tentative)
            .SumAsync(group => group.DepositAmount);

        RecentOpenLeads = await _context.SalesLeads
            .Include(lead => lead.SalesAccount)
            .AsNoTracking()
            .Where(lead => openStatuses.Contains(lead.Status))
            .OrderBy(lead => lead.ExpectedCloseDate ?? DateTime.MaxValue)
            .ThenBy(lead => lead.LeadName)
            .Take(10)
            .ToListAsync();

        FollowUps = await _context.SalesActivities
            .Include(activity => activity.SalesAccount)
            .Include(activity => activity.SalesLead)
            .AsNoTracking()
            .Where(activity =>
                activity.NextFollowUpDate >= today &&
                activity.NextFollowUpDate < followUpEnd)
            .OrderBy(activity => activity.NextFollowUpDate)
            .Take(10)
            .ToListAsync();
    }

}
