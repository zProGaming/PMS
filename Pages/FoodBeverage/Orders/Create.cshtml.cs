using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FoodBeverage.Orders;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public POSOrder Order { get; set; } = new()
    {
        OrderType = POSOrderType.DineIn,
        OrderStatus = POSOrderStatus.Open,
        PaymentStatus = POSPaymentStatus.Unpaid
    };

    public SelectList OutletOptions { get; set; } = default!;

    public SelectList TableOptions { get; set; } = default!;

    public SelectList ReservationOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> OrderTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await ApplyReservationGuestAsync();
        await ValidateOrderAsync();

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(Order.OutletId, Order.DiningTableId, Order.ReservationId, Order.OrderType);
            return Page();
        }

        Order.OrderNumber = CreateOrderNumber();
        Order.OrderDate = DateTime.Now;
        Order.OrderStatus = POSOrderStatus.Open;
        Order.PaymentStatus = POSPaymentStatus.Unpaid;
        Order.CreatedBy = User.Identity?.Name ?? Environment.UserName;

        if (Order.OrderType == POSOrderType.DineIn && Order.DiningTableId is not null)
        {
            var table = await _context.DiningTables.FindAsync(Order.DiningTableId);
            if (table is not null)
            {
                table.Status = DiningTableStatus.Occupied;
            }
        }

        _context.POSOrders.Add(Order);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id = Order.Id });
    }

    private async Task ApplyReservationGuestAsync()
    {
        if (Order.ReservationId is null)
        {
            Order.GuestId = null;
            return;
        }

        var reservation = await _context.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(reservation => reservation.Id == Order.ReservationId);

        Order.GuestId = reservation?.GuestId;
    }

    private async Task ValidateOrderAsync()
    {
        if (Order.OrderType == POSOrderType.DineIn && Order.DiningTableId is null)
        {
            ModelState.AddModelError("Order.DiningTableId", "A dining table is required for dine-in orders.");
        }

        if (Order.OrderType != POSOrderType.DineIn)
        {
            Order.DiningTableId = null;
        }

        if (Order.OrderType == POSOrderType.RoomService && Order.ReservationId is null)
        {
            ModelState.AddModelError("Order.ReservationId", "A checked-in reservation is required for room service orders.");
        }

        if ((Order.OrderType == POSOrderType.RoomService || Order.ReservationId is not null) && Order.ReservationId is not null)
        {
            var isCheckedIn = await _context.Reservations.AnyAsync(reservation =>
                reservation.Id == Order.ReservationId &&
                reservation.Status == ReservationStatus.CheckedIn);

            if (!isCheckedIn)
            {
                ModelState.AddModelError("Order.ReservationId", "Room service and charge-to-room orders require a checked-in reservation.");
            }
        }
    }

    private async Task LoadOptionsAsync(
        object? selectedOutlet = null,
        object? selectedTable = null,
        object? selectedReservation = null,
        POSOrderType selectedOrderType = POSOrderType.DineIn)
    {
        var outlets = await _context.Outlets
            .AsNoTracking()
            .Where(outlet => outlet.IsActive)
            .OrderBy(outlet => outlet.Name)
            .Select(outlet => new { outlet.Id, outlet.Name })
            .ToListAsync();

        var tables = await _context.DiningTables
            .Include(table => table.Outlet)
            .AsNoTracking()
            .Where(table => table.Status == DiningTableStatus.Available || table.Id == Order.DiningTableId)
            .OrderBy(table => table.Outlet!.Name)
            .ThenBy(table => table.TableName)
            .ToListAsync();

        var reservations = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .AsNoTracking()
            .Where(reservation => reservation.Status == ReservationStatus.CheckedIn)
            .OrderBy(reservation => reservation.Room!.RoomNumber)
            .ToListAsync();

        OutletOptions = new SelectList(outlets, "Id", "Name", selectedOutlet);
        TableOptions = new SelectList(
            tables.Select(table => new { table.Id, Name = $"{table.Outlet?.Name} - {table.TableName}" }),
            "Id",
            "Name",
            selectedTable);
        ReservationOptions = new SelectList(
            reservations.Select(reservation => new
            {
                reservation.Id,
                Name = $"{reservation.Room?.RoomNumber} - {reservation.Guest?.FirstName} {reservation.Guest?.LastName} ({reservation.ConfirmationNumber})"
            }),
            "Id",
            "Name",
            selectedReservation);
        OrderTypeOptions = Enum.GetValues<POSOrderType>().Select(type => new SelectListItem
        {
            Value = type.ToString(),
            Text = type.ToString(),
            Selected = type == selectedOrderType
        });
    }

    private static string CreateOrderNumber()
    {
        return $"POS-{DateTime.Now:yyyyMMddHHmmssfff}";
    }
}
