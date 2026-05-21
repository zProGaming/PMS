using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.FoodBeverage.Orders;

public enum POSSettlementMethod
{
    Cash = 0,
    Card = 1,
    EWallet = 2,
    ChargeToRoom = 3
}

public class CloseModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public POSSettlementMethod SettlementMethod { get; set; } = POSSettlementMethod.Cash;

    [BindProperty]
    public int? SelectedReservationId { get; set; }

    public POSOrder Order { get; set; } = default!;

    public SelectList ReservationOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> SettlementMethodOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var order = await LoadOrderAsync(id.Value, asTracking: false);
        if (order is null)
        {
            return NotFound();
        }

        Order = order;
        SelectedReservationId = order.ReservationId;
        await LoadOptionsAsync(SelectedReservationId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var order = await LoadOrderAsync(id.Value, asTracking: true);
        if (order is null)
        {
            return NotFound();
        }

        Order = order;
        POSOrderTotalsCalculator.Recalculate(Order);
        ValidateCanClose();

        if (SettlementMethod == POSSettlementMethod.ChargeToRoom)
        {
            await ValidateAndApplyChargeToRoomAsync();
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(SelectedReservationId);
            return Page();
        }

        Order.OrderStatus = POSOrderStatus.Closed;
        Order.PaymentStatus = SettlementMethod == POSSettlementMethod.ChargeToRoom
            ? POSPaymentStatus.ChargedToRoom
            : POSPaymentStatus.Paid;
        Order.ClosedAt = DateTime.Now;
        Order.Notes = AppendSettlementNote(Order.Notes, SettlementMethod);

        if (Order.DiningTable is not null)
        {
            Order.DiningTable.Status = DiningTableStatus.Cleaning;
        }

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id = Order.Id });
    }

    private void ValidateCanClose()
    {
        if (Order.OrderStatus is POSOrderStatus.Closed or POSOrderStatus.Cancelled)
        {
            ModelState.AddModelError(string.Empty, "This order is already closed or cancelled.");
        }

        if (!Order.Items.Any(item => !item.IsVoided))
        {
            ModelState.AddModelError(string.Empty, "Add at least one active item before closing the order.");
        }

        if (Order.TotalAmount <= 0)
        {
            ModelState.AddModelError(string.Empty, "Order total must be greater than zero before settlement.");
        }
    }

    private async Task ValidateAndApplyChargeToRoomAsync()
    {
        var reservationId = SelectedReservationId ?? Order.ReservationId;
        if (reservationId is null)
        {
            ModelState.AddModelError("SelectedReservationId", "Select a checked-in reservation for charge to room.");
            return;
        }

        var reservation = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .FirstOrDefaultAsync(reservation =>
                reservation.Id == reservationId &&
                reservation.Status == ReservationStatus.CheckedIn);

        if (reservation is null)
        {
            ModelState.AddModelError("SelectedReservationId", "Charge to room is allowed only for checked-in reservations.");
            return;
        }

        var folio = await _context.Folios
            .FirstOrDefaultAsync(folio => folio.ReservationId == reservation.Id);

        if (folio is null)
        {
            folio = new Folio
            {
                PropertyId = reservation.PropertyId,
                ReservationId = reservation.Id,
                GuestId = reservation.GuestId,
                FolioNumber = $"FOL-{reservation.Id:000000}",
                Status = FolioStatus.Open,
                OpenedAtUtc = DateTime.UtcNow
            };

            _context.Folios.Add(folio);
        }
        else
        {
            var orderToken = $"Order #{Order.OrderNumber}";
            var alreadyChargedToFolio = await _context.FolioItems
                .AsNoTracking()
                .AnyAsync(item =>
                    item.FolioId == folio.Id &&
                    !item.IsVoided &&
                    item.ChargeCode == "FNB" &&
                    item.Description.Contains(orderToken));

            if (alreadyChargedToFolio)
            {
                ModelState.AddModelError(string.Empty, "This POS order has already been charged to the guest folio.");
                return;
            }
        }

        var businessDate = await GetBusinessDateAsync();

        _context.FolioItems.Add(new FolioItem
        {
            Folio = folio,
            Description = $"F&B Charge - {Order.Outlet?.Name} - Order #{Order.OrderNumber}",
            ChargeCode = "FNB",
            Quantity = 1,
            UnitPrice = Order.TotalAmount,
            Amount = Order.TotalAmount,
            PostingDate = businessDate.Date.Add(DateTime.Now.TimeOfDay),
            PostedBy = User.Identity?.Name ?? "F&B POS",
            IsVoided = false
        });

        Order.ReservationId = reservation.Id;
        Order.GuestId = reservation.GuestId;
    }

    private async Task<POSOrder?> LoadOrderAsync(int id, bool asTracking)
    {
        var query = _context.POSOrders
            .Include(order => order.Outlet)
            .Include(order => order.DiningTable)
            .Include(order => order.Guest)
            .Include(order => order.Reservation)
                .ThenInclude(reservation => reservation!.Room)
            .Include(order => order.Items)
                .ThenInclude(item => item.MenuItem)
            .Where(order => order.Id == id);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.AsSplitQuery().FirstOrDefaultAsync();
    }

    private async Task LoadOptionsAsync(object? selectedReservation = null)
    {
        var reservations = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .AsNoTracking()
            .Where(reservation => reservation.Status == ReservationStatus.CheckedIn)
            .OrderBy(reservation => reservation.Room != null ? reservation.Room.RoomNumber : string.Empty)
            .ToListAsync();

        ReservationOptions = new SelectList(
            reservations.Select(reservation => new
            {
                reservation.Id,
                Name = $"{reservation.Room?.RoomNumber} - {reservation.Guest?.FirstName} {reservation.Guest?.LastName} ({reservation.ConfirmationNumber})"
            }),
            "Id",
            "Name",
            selectedReservation);

        SettlementMethodOptions = Enum.GetValues<POSSettlementMethod>().Select(method => new SelectListItem
        {
            Value = method.ToString(),
            Text = method == POSSettlementMethod.EWallet ? "E-wallet" : method.ToString(),
            Selected = method == SettlementMethod
        });
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }

    private static string AppendSettlementNote(string? notes, POSSettlementMethod method)
    {
        var settlementNote = $"Settlement: {(method == POSSettlementMethod.EWallet ? "E-wallet" : method)}";

        return string.IsNullOrWhiteSpace(notes)
            ? settlementNote
            : $"{notes}{Environment.NewLine}{settlementNote}";
    }
}
