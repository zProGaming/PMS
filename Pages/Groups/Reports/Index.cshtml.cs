using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Groups.Reports;

public class IndexModel(ApplicationDbContext context, GroupManagementService groupManagementService) : PageModel
{
    public IList<GroupBooking> Groups { get; private set; } = [];
    public IList<GroupRoomBlock> RoomBlocks { get; private set; } = [];
    public IList<GroupFolio> GroupFolios { get; private set; } = [];
    public IList<PseudoRoom> PseudoRooms { get; private set; } = [];
    public IList<ChargeRoutingRule> RoutingRules { get; private set; } = [];
    public Dictionary<int, GroupCollectionSummary> CollectionSummaries { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Groups = await context.GroupBookings
            .Include(item => item.SalesAccount)
            .Include(item => item.RoomBlocks).ThenInclude(item => item.RoomType)
            .Include(item => item.Members).ThenInclude(item => item.Reservation).ThenInclude(item => item!.Guest)
            .Include(item => item.GroupFolios)
            .Include(item => item.Deposits)
            .AsNoTracking()
            .OrderByDescending(item => item.ArrivalDate)
            .Take(100)
            .ToListAsync();

        RoomBlocks = await context.GroupRoomBlocks
            .Include(item => item.GroupBooking)
            .Include(item => item.RoomType)
            .AsNoTracking()
            .OrderBy(item => item.BlockDate)
            .ThenBy(item => item.GroupBooking!.GroupCode)
            .Take(250)
            .ToListAsync();

        GroupFolios = await context.GroupFolios
            .Include(item => item.GroupBooking)
            .Include(item => item.PseudoRoom)
            .Include(item => item.Folio).ThenInclude(item => item!.Items)
            .Include(item => item.Folio).ThenInclude(item => item!.Payments)
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync();

        PseudoRooms = await context.PseudoRooms
            .Include(item => item.LinkedGroupBooking)
            .AsNoTracking()
            .OrderBy(item => item.PseudoRoomCode)
            .Take(100)
            .ToListAsync();

        RoutingRules = await context.ChargeRoutingRules
            .Include(item => item.GroupBooking)
            .Include(item => item.TargetGroupFolio)
            .Include(item => item.TargetPseudoRoom)
            .AsNoTracking()
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.SourceChargeCategory)
            .Take(100)
            .ToListAsync();

        foreach (var group in Groups)
        {
            CollectionSummaries[group.Id] = await groupManagementService.GetCollectionSummaryAsync(group.Id);
        }
    }
}
