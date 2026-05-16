using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Groups;

namespace Vantage.PMS.Pages.Groups.Collections;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<GroupCollectionRow> Groups { get; private set; } = [];

    public decimal TotalDeposits { get; private set; }

    public decimal TotalAllocated { get; private set; }

    public decimal TotalOutstanding { get; private set; }

    public int GroupsWithOutstandingBalance { get; private set; }

    public async Task OnGetAsync()
    {
        var groups = await context.GroupBookings
            .Include(group => group.Deposits)
            .Include(group => group.PaymentAllocations)
            .Include(group => group.RoomBlocks)
            .Include(group => group.GroupFolios).ThenInclude(groupFolio => groupFolio.Folio).ThenInclude(folio => folio!.Items)
            .Include(group => group.GroupFolios).ThenInclude(groupFolio => groupFolio.Folio).ThenInclude(folio => folio!.Payments)
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(group => group.ArrivalDate)
            .Take(150)
            .ToListAsync();

        Groups = groups.Select(group =>
            {
                var depositTotal = group.Deposits
                    .Where(deposit => deposit.Status != GroupDepositStatus.Cancelled)
                    .Sum(deposit => deposit.Amount);
                var allocatedTotal = group.PaymentAllocations.Sum(allocation => allocation.AllocatedAmount);
                var availableDeposit = depositTotal - allocatedTotal;
                var postedCharges = group.GroupFolios
                    .Where(groupFolio => groupFolio.Folio is not null)
                    .Sum(groupFolio => groupFolio.Folio!.Items.Where(item => !item.IsVoided).Sum(item => item.Amount));
                var postedPayments = group.GroupFolios
                    .Where(groupFolio => groupFolio.Folio is not null)
                    .Sum(groupFolio => groupFolio.Folio!.Payments.Sum(payment => payment.Amount));
                var groupFolioBalance = postedCharges - postedPayments;
                var outstandingBalance = Math.Max(groupFolioBalance - Math.Max(availableDeposit, 0), 0);
                var blocked = group.RoomBlocks.Sum(block => block.RoomsBlocked);
                var pickedUp = group.RoomBlocks.Sum(block => block.RoomsPickedUp);

                return new GroupCollectionRow
                {
                    GroupBookingId = group.Id,
                    GroupCode = group.GroupCode,
                    GroupName = group.GroupName,
                    BookingStatus = group.BookingStatus,
                    ArrivalDate = group.ArrivalDate,
                    DepartureDate = group.DepartureDate,
                    RoomsBlocked = blocked,
                    RoomsPickedUp = pickedUp,
                    DepositTotal = depositTotal,
                    AllocatedTotal = allocatedTotal,
                    AvailableDeposit = availableDeposit,
                    PostedCharges = postedCharges,
                    PostedPayments = postedPayments,
                    GroupFolioBalance = groupFolioBalance,
                    OutstandingBalance = outstandingBalance
                };
            })
            .ToList();

        TotalDeposits = Groups.Sum(group => group.DepositTotal);
        TotalAllocated = Groups.Sum(group => group.AllocatedTotal);
        TotalOutstanding = Groups.Sum(group => group.OutstandingBalance);
        GroupsWithOutstandingBalance = Groups.Count(group => group.OutstandingBalance > 0);
    }
}

public class GroupCollectionRow
{
    public int GroupBookingId { get; set; }

    public string GroupCode { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public GroupBookingStatus BookingStatus { get; set; }

    public DateTime ArrivalDate { get; set; }

    public DateTime DepartureDate { get; set; }

    public int RoomsBlocked { get; set; }

    public int RoomsPickedUp { get; set; }

    public decimal DepositTotal { get; set; }

    public decimal AllocatedTotal { get; set; }

    public decimal AvailableDeposit { get; set; }

    public decimal PostedCharges { get; set; }

    public decimal PostedPayments { get; set; }

    public decimal GroupFolioBalance { get; set; }

    public decimal OutstandingBalance { get; set; }
}
