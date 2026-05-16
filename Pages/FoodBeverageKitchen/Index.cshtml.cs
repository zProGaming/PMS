using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.FoodBeverageKitchen;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private static readonly TimeSpan DelayedThreshold = TimeSpan.FromMinutes(20);
    private readonly ApplicationDbContext _context = context;

    public int NewItems { get; set; }

    public int PreparingItems { get; set; }

    public int ReadyItems { get; set; }

    public int DelayedItems { get; set; }

    public int ServedItemsToday { get; set; }

    public IList<KitchenStationGroup> StationGroups { get; set; } = new List<KitchenStationGroup>();

    public async Task OnGetAsync()
    {
        await LoadDashboardAsync();
    }

    public async Task<IActionResult> OnPostMarkPreparingAsync(int itemId)
    {
        return await UpdateItemStatusAsync(itemId, POSOrderItemStatus.Preparing);
    }

    public async Task<IActionResult> OnPostMarkReadyAsync(int itemId)
    {
        return await UpdateItemStatusAsync(itemId, POSOrderItemStatus.Ready);
    }

    public async Task<IActionResult> OnPostMarkServedAsync(int itemId)
    {
        return await UpdateItemStatusAsync(itemId, POSOrderItemStatus.Served);
    }

    public async Task<IActionResult> OnPostMarkCancelledAsync(int itemId)
    {
        return await UpdateItemStatusAsync(itemId, POSOrderItemStatus.Cancelled);
    }

    private async Task LoadDashboardAsync()
    {
        var now = DateTime.Now;
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        ServedItemsToday = await _context.POSOrderItems.CountAsync(item =>
            !item.IsVoided &&
            item.ItemStatus == POSOrderItemStatus.Served &&
            item.ServedAt >= today &&
            item.ServedAt < tomorrow);

        var items = await _context.POSOrderItems
            .Include(item => item.POSOrder)
                .ThenInclude(order => order!.Outlet)
            .Include(item => item.POSOrder)
                .ThenInclude(order => order!.DiningTable)
            .Include(item => item.POSOrder)
                .ThenInclude(order => order!.Guest)
            .Include(item => item.POSOrder)
                .ThenInclude(order => order!.Reservation)
                    .ThenInclude(reservation => reservation!.Room)
            .Include(item => item.POSOrder)
                .ThenInclude(order => order!.Reservation)
                    .ThenInclude(reservation => reservation!.Guest)
            .Include(item => item.MenuItem)
                .ThenInclude(menuItem => menuItem!.KitchenStation)
            .AsNoTracking()
            .AsSplitQuery()
            .Where(item =>
                item.POSOrder != null &&
                item.POSOrder.OrderStatus != POSOrderStatus.Closed &&
                item.POSOrder.OrderStatus != POSOrderStatus.Cancelled &&
                item.POSOrder.OrderStatus != POSOrderStatus.Open &&
                !item.IsVoided &&
                (item.ItemStatus == POSOrderItemStatus.New ||
                 item.ItemStatus == POSOrderItemStatus.Preparing ||
                 item.ItemStatus == POSOrderItemStatus.Ready))
            .ToListAsync();

        var rows = items
            .Select(item => BuildRow(item, now))
            .OrderBy(row => row.StationName)
            .ThenBy(row => row.SortDate)
            .ToList();

        NewItems = rows.Count(row => row.ItemStatus == POSOrderItemStatus.New);
        PreparingItems = rows.Count(row => row.ItemStatus == POSOrderItemStatus.Preparing);
        ReadyItems = rows.Count(row => row.ItemStatus == POSOrderItemStatus.Ready);
        DelayedItems = rows.Count(row => row.IsDelayed);

        StationGroups = rows
            .GroupBy(row => row.StationName)
            .Select(group => new KitchenStationGroup(group.Key, group.ToList()))
            .ToList();
    }

    private async Task<IActionResult> UpdateItemStatusAsync(int itemId, POSOrderItemStatus status)
    {
        var item = await _context.POSOrderItems
            .Include(item => item.POSOrder)
                .ThenInclude(order => order!.Items)
                    .ThenInclude(orderItem => orderItem.MenuItem)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item => item.Id == itemId);

        if (item is null || item.POSOrder is null)
        {
            return NotFound();
        }

        if (item.POSOrder.OrderStatus is POSOrderStatus.Closed or POSOrderStatus.Cancelled)
        {
            return RedirectToPage();
        }

        ApplyStatus(item, status);
        UpdateOrderStatus(item.POSOrder);

        if (status == POSOrderItemStatus.Cancelled)
        {
            POSOrderTotalsCalculator.Recalculate(item.POSOrder);
        }

        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    private static void ApplyStatus(POSOrderItem item, POSOrderItemStatus status)
    {
        var now = DateTime.Now;
        item.SentToKitchenAt ??= now;

        switch (status)
        {
            case POSOrderItemStatus.Preparing:
                item.ItemStatus = POSOrderItemStatus.Preparing;
                item.PreparingAt ??= now;
                break;
            case POSOrderItemStatus.Ready:
                item.ItemStatus = POSOrderItemStatus.Ready;
                item.PreparingAt ??= now;
                item.ReadyAt ??= now;
                break;
            case POSOrderItemStatus.Served:
                item.ItemStatus = POSOrderItemStatus.Served;
                item.PreparingAt ??= now;
                item.ReadyAt ??= now;
                item.ServedAt ??= now;
                break;
            case POSOrderItemStatus.Cancelled:
                item.ItemStatus = POSOrderItemStatus.Cancelled;
                item.IsVoided = true;
                item.CancelledAt ??= now;
                break;
        }
    }

    private static void UpdateOrderStatus(POSOrder order)
    {
        var activeItems = order.Items
            .Where(item => !item.IsVoided &&
                           item.ItemStatus != POSOrderItemStatus.Cancelled &&
                           item.ItemStatus != POSOrderItemStatus.Voided)
            .ToList();

        if (activeItems.Count == 0)
        {
            order.OrderStatus = POSOrderStatus.Cancelled;
            order.PaymentStatus = POSPaymentStatus.Voided;
            return;
        }

        if (activeItems.All(item => item.ItemStatus == POSOrderItemStatus.Served))
        {
            order.OrderStatus = POSOrderStatus.Served;
        }
        else if (activeItems.All(item => item.ItemStatus is POSOrderItemStatus.Ready or POSOrderItemStatus.Served))
        {
            order.OrderStatus = POSOrderStatus.Ready;
        }
        else if (activeItems.Any(item => item.ItemStatus == POSOrderItemStatus.Preparing))
        {
            order.OrderStatus = POSOrderStatus.Preparing;
        }
        else
        {
            order.OrderStatus = POSOrderStatus.SentToKitchen;
        }
    }

    private static KitchenOrderItemRow BuildRow(POSOrderItem item, DateTime now)
    {
        var sentToKitchenAt = item.SentToKitchenAt ?? item.POSOrder!.OrderDate;
        var isDelayed = item.ItemStatus is POSOrderItemStatus.New or POSOrderItemStatus.Preparing &&
                        sentToKitchenAt <= now.Subtract(DelayedThreshold);

        return new KitchenOrderItemRow(
            item.Id,
            item.POSOrderId,
            item.POSOrder!.OrderNumber,
            item.POSOrder.Outlet?.Name ?? "-",
            item.POSOrder.DiningTable?.TableName ?? "-",
            BuildGuestRoomInfo(item.POSOrder),
            item.MenuItem?.KitchenStation?.Name ?? "Unassigned Station",
            item.MenuItem?.Name ?? "-",
            item.Quantity,
            item.Notes,
            item.ItemStatus,
            item.SentToKitchenAt,
            item.PreparingAt,
            item.ReadyAt,
            item.ServedAt,
            item.CancelledAt,
            isDelayed,
            FormatElapsedTime(now - sentToKitchenAt),
            sentToKitchenAt);
    }

    private static string BuildGuestRoomInfo(POSOrder order)
    {
        var guest = order.Guest ?? order.Reservation?.Guest;
        var guestName = $"{guest?.FirstName} {guest?.LastName}".Trim();
        var roomNumber = order.Reservation?.Room?.RoomNumber;

        if (string.IsNullOrWhiteSpace(guestName) && string.IsNullOrWhiteSpace(roomNumber))
        {
            return "-";
        }

        if (string.IsNullOrWhiteSpace(roomNumber))
        {
            return guestName;
        }

        if (string.IsNullOrWhiteSpace(guestName))
        {
            return $"Room {roomNumber}";
        }

        return $"Room {roomNumber} - {guestName}";
    }

    private static string FormatElapsedTime(TimeSpan elapsed)
    {
        if (elapsed.TotalMinutes < 1)
        {
            return "Less than 1 min";
        }

        if (elapsed.TotalHours < 1)
        {
            return $"{(int)elapsed.TotalMinutes} min";
        }

        return $"{(int)elapsed.TotalHours} hr {elapsed.Minutes} min";
    }

    public record KitchenStationGroup(string StationName, IList<KitchenOrderItemRow> Items);

    public record KitchenOrderItemRow(
        int OrderItemId,
        int OrderId,
        string OrderNumber,
        string OutletName,
        string TableName,
        string GuestRoomInfo,
        string StationName,
        string ItemName,
        decimal Quantity,
        string? Notes,
        POSOrderItemStatus ItemStatus,
        DateTime? SentToKitchenAt,
        DateTime? PreparingAt,
        DateTime? ReadyAt,
        DateTime? ServedAt,
        DateTime? CancelledAt,
        bool IsDelayed,
        string ElapsedTime,
        DateTime SortDate);
}
