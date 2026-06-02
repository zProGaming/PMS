using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Reservations;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Reservation Reservation { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        return await LoadReservationOrNotFoundAsync(id);
    }

    public async Task<IActionResult> OnGetNativeAsync(int? id)
    {
        var result = await LoadReservationOrNotFoundAsync(id);
        return result is NotFoundResult ? result : NativePartial("_CancelNative");
    }

    public async Task<IActionResult> OnGetNoShowNativeAsync(int? id)
    {
        var result = await LoadReservationOrNotFoundAsync(id);
        return result is NotFoundResult ? result : NativePartial("_NoShowNative");
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation is not null)
        {
            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostNoShowAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation is not null)
        {
            reservation.Status = ReservationStatus.NoShow;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }

    private async Task<IActionResult> LoadReservationOrNotFoundAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservation = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .AsNoTracking()
            .FirstOrDefaultAsync(reservation => reservation.Id == id);

        if (reservation is null)
        {
            return NotFound();
        }

        Reservation = reservation;
        return Page();
    }

    private PartialViewResult NativePartial(string partialName)
    {
        return new PartialViewResult
        {
            ViewName = partialName,
            ViewData = new ViewDataDictionary<DeleteModel>(ViewData, this)
        };
    }
}
