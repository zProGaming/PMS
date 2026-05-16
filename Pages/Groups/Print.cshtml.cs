using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Groups;

public class PrintModel(ApplicationDbContext context, GroupManagementService groupManagementService) : PageModel
{
    public string PrintType { get; private set; } = string.Empty;
    public string PrintTitle { get; private set; } = "Group Document";
    public GroupBooking? GroupBooking { get; private set; }
    public PseudoRoom? PseudoRoom { get; private set; }
    public GroupCollectionSummary CollectionSummary { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string type, int id)
    {
        PrintType = type;
        PrintTitle = type switch
        {
            "group-confirmation" => "Group Confirmation",
            "rooming-list" => "Group Rooming List",
            "group-master-folio" => "Group Master Folio",
            "collection-statement" => "Group Collection Statement",
            "pseudo-room-folio" => "Pseudo Room Folio",
            "billing-instruction" => "Group Billing Instruction Sheet",
            _ => "Group Document"
        };

        if (type == "pseudo-room-folio")
        {
            PseudoRoom = await context.PseudoRooms
                .Include(item => item.LinkedGroupBooking)
                .Include(item => item.LinkedSalesAccount)
                .Include(item => item.LinkedBanquetEvent)
                .Include(item => item.GroupFolios).ThenInclude(item => item.GroupBooking)
                .Include(item => item.GroupFolios).ThenInclude(item => item.Folio).ThenInclude(item => item!.Items)
                .Include(item => item.GroupFolios).ThenInclude(item => item.Folio).ThenInclude(item => item!.Payments)
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id);

            return PseudoRoom is null ? NotFound() : Page();
        }

        GroupBooking = await context.GroupBookings
            .Include(item => item.SalesAccount)
            .Include(item => item.RoomBlocks).ThenInclude(item => item.RoomType)
            .Include(item => item.RoomBlocks).ThenInclude(item => item.RatePlan)
            .Include(item => item.Members).ThenInclude(item => item.Reservation).ThenInclude(item => item!.Guest)
            .Include(item => item.Members).ThenInclude(item => item.Reservation).ThenInclude(item => item!.Room)
            .Include(item => item.Members).ThenInclude(item => item.Reservation).ThenInclude(item => item!.RoomType)
            .Include(item => item.Members).ThenInclude(item => item.Reservation).ThenInclude(item => item!.Folios).ThenInclude(item => item.Items)
            .Include(item => item.Members).ThenInclude(item => item.Reservation).ThenInclude(item => item!.Folios).ThenInclude(item => item.Payments)
            .Include(item => item.GroupFolios).ThenInclude(item => item.PseudoRoom)
            .Include(item => item.GroupFolios).ThenInclude(item => item.Folio).ThenInclude(item => item!.Items)
            .Include(item => item.GroupFolios).ThenInclude(item => item.Folio).ThenInclude(item => item!.Payments)
            .Include(item => item.Deposits)
            .Include(item => item.PaymentAllocations)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (GroupBooking is null)
        {
            return NotFound();
        }

        CollectionSummary = await groupManagementService.GetCollectionSummaryAsync(id);
        return Page();
    }
}
