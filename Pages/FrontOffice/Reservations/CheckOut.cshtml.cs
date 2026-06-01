using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Reservations;

public class CheckOutModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Reservation Reservation { get; set; } = default!;

    [BindProperty]
    public bool ManagerOverrideRequested { get; set; }

    public int? FolioId { get; set; }

    public decimal FolioBalance { get; set; }

    public bool HasOutstandingBalance => FolioBalance > 0;

    public bool CanUseManagerOverride =>
        User.IsInRole(PmsRoles.SystemAdmin) ||
        User.IsInRole(PmsRoles.GeneralManager) ||
        User.IsInRole(PmsRoles.FrontOfficeManager) ||
        User.IsInRole(PmsRoles.FinanceManager);

    public bool BalanceOverrideAccepted => HasOutstandingBalance && ManagerOverrideRequested && CanUseManagerOverride;

    public bool CanSubmitCheckOut =>
        Reservation.Status == ReservationStatus.CheckedIn &&
        Reservation.RoomId is not null;

    public bool CanCheckOut =>
        CanSubmitCheckOut &&
        (!HasOutstandingBalance || BalanceOverrideAccepted);

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var loadResult = await LoadCheckOutFormAsync(id);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return Page();
    }

    public async Task<IActionResult> OnGetNativeAsync(int? id)
    {
        var loadResult = await LoadCheckOutFormAsync(id);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return NativePartial();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservation = await LoadReservationAsync(id.Value, asTracking: true);
        if (reservation is null)
        {
            return NotFound();
        }

        Reservation = reservation;
        LoadFolioState();
        Reservation.ManagerOverrideRequested = ManagerOverrideRequested && CanUseManagerOverride;
        ValidateCanCheckOut();

        if (!ModelState.IsValid)
        {
            return NativePartialOrPage();
        }

        Reservation.Status = ReservationStatus.CheckedOut;
        Reservation.ActualCheckOutDate = DateTime.Now;
        Reservation.Room!.Status = RoomStatus.Dirty;
        foreach (var folio in Reservation.Folios.Where(folio => folio.Status == FolioStatus.Open && folio.Balance <= 0))
        {
            folio.Status = FolioStatus.Closed;
            folio.ClosedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id = Reservation.Id });
    }

    private async Task<IActionResult?> LoadCheckOutFormAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservation = await LoadReservationAsync(id.Value, asTracking: false);
        if (reservation is null)
        {
            return NotFound();
        }

        Reservation = reservation;
        LoadFolioState();
        ManagerOverrideRequested = Reservation.ManagerOverrideRequested;
        ValidateCanCheckOut();

        return null;
    }

    private async Task<Reservation?> LoadReservationAsync(int id, bool asTracking)
    {
        var query = _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .Include(reservation => reservation.Folios)
                .ThenInclude(folio => folio.Items)
            .Include(reservation => reservation.Folios)
                .ThenInclude(folio => folio.Payments)
            .Where(reservation => reservation.Id == id);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private void LoadFolioState()
    {
        var folio = Reservation.Folios.FirstOrDefault();
        FolioId = folio?.Id;
        FolioBalance = folio?.Balance ?? 0;
    }

    private void ValidateCanCheckOut()
    {
        if (Reservation.Status != ReservationStatus.CheckedIn)
        {
            ModelState.AddModelError(string.Empty, "Only checked-in reservations can be checked out.");
        }

        if (Reservation.RoomId is null || Reservation.Room is null)
        {
            ModelState.AddModelError(string.Empty, "A room must be assigned before check-out.");
        }

        if (HasOutstandingBalance && !ManagerOverrideRequested)
        {
            ModelState.AddModelError(string.Empty, "Guest has outstanding balance. Please settle the folio before check-out or request an authorized manager override.");
        }

        if (HasOutstandingBalance && ManagerOverrideRequested && !CanUseManagerOverride)
        {
            ModelState.AddModelError(string.Empty, "Only SystemAdmin, GeneralManager, FrontOfficeManager, or FinanceManager can override checkout with an outstanding balance.");
        }
    }

    private IActionResult NativePartialOrPage()
    {
        return IsNativeWorkflowRequest() ? NativePartial() : Page();
    }

    private bool IsNativeWorkflowRequest()
    {
        return string.Equals(Request.Query["vpmsNative"], "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-VPMS-Native-Dialog"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private PartialViewResult NativePartial()
    {
        return new PartialViewResult
        {
            ViewName = "_CheckOutNative",
            ViewData = new ViewDataDictionary<CheckOutModel>(ViewData, this)
        };
    }
}
