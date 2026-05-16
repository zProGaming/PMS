using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.GuestPortalManagement.ExpressCheckout;

public class IndexModel(ApplicationDbContext context, GuestPortalService guestPortalService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly GuestPortalService _guestPortalService = guestPortalService;

    public IList<ExpressCheckoutRequest> Requests { get; set; } = new List<ExpressCheckoutRequest>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Requests = await _context.ExpressCheckoutRequests
            .Include(request => request.Guest)
            .Include(request => request.Reservation)
                .ThenInclude(reservation => reservation!.Room)
            .Include(request => request.Reservation)
                .ThenInclude(reservation => reservation!.Folios)
                    .ThenInclude(folio => folio.Items)
            .Include(request => request.Reservation)
                .ThenInclude(reservation => reservation!.Folios)
                    .ThenInclude(folio => folio.Payments)
            .AsNoTracking()
            .AsSplitQuery()
            .OrderBy(request => request.Status == ExpressCheckoutRequestStatus.Completed)
            .ThenByDescending(request => request.RequestedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int id, ExpressCheckoutRequestStatus status)
    {
        var request = await _context.ExpressCheckoutRequests.FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        request.Status = status;
        request.ProcessedAt = DateTime.Now;
        request.ProcessedBy = User.Identity?.Name;
        await _context.SaveChangesAsync();
        StatusMessage = $"Express checkout request marked {status}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteCheckoutAsync(int id)
    {
        var result = await _guestPortalService.CompleteExpressCheckoutAsync(id, User.Identity?.Name);
        StatusMessage = result.Message;
        return RedirectToPage();
    }

    public decimal GetFolioBalance(ExpressCheckoutRequest request)
    {
        return request.Reservation?.Folios.FirstOrDefault()?.Balance ?? 0;
    }
}
