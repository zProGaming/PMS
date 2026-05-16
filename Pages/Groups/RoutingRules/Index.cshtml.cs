using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Groups.RoutingRules;

public class IndexModel(ApplicationDbContext context, AuditLogService auditLogService) : PageModel
{
    public IList<ChargeRoutingRule> Rules { get; private set; } = [];
    public SelectList GroupBookingOptions { get; private set; } = null!;
    public SelectList ReservationOptions { get; private set; } = null!;
    public SelectList FolioOptions { get; private set; } = null!;
    public SelectList GroupFolioOptions { get; private set; } = null!;
    public SelectList PseudoRoomOptions { get; private set; } = null!;

    [BindProperty]
    public ChargeRoutingRule Input { get; set; } = new() { IsActive = true };

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        Input.IsActive = true;

        if (Input.GroupBookingId is null && Input.ReservationId is null && Input.FolioId is null)
        {
            ModelState.AddModelError(string.Empty, "Select a source group, reservation, or folio.");
        }

        if (Input.RouteToType == RouteToType.GroupMasterFolio && Input.TargetGroupFolioId is null)
        {
            ModelState.AddModelError("Input.TargetGroupFolioId", "Target group master folio is required.");
        }

        if (Input.RouteToType == RouteToType.GuestFolio && Input.TargetFolioId is null)
        {
            ModelState.AddModelError("Input.TargetFolioId", "Target folio is required.");
        }

        if (Input.RouteToType == RouteToType.PseudoRoomFolio && Input.TargetPseudoRoomId is null && Input.TargetGroupFolioId is null)
        {
            ModelState.AddModelError("Input.TargetPseudoRoomId", "Select a pseudo room or a linked group folio.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        context.ChargeRoutingRules.Add(Input);
        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Create, "Group Management", nameof(ChargeRoutingRule), Input.Id.ToString(), null, new { Input.GroupBookingId, Input.ReservationId, Input.FolioId, Input.SourceChargeCategory, Input.RouteToType });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var rule = await context.ChargeRoutingRules.FindAsync(id);
        if (rule is not null)
        {
            rule.IsActive = !rule.IsActive;
            await context.SaveChangesAsync();
            await auditLogService.LogAsync(AuditActionType.Update, "Group Management", nameof(ChargeRoutingRule), rule.Id.ToString(), null, new { rule.IsActive });
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Rules = await context.ChargeRoutingRules
            .Include(item => item.GroupBooking)
            .Include(item => item.Reservation).ThenInclude(item => item!.Guest)
            .Include(item => item.Folio).ThenInclude(item => item!.Guest)
            .Include(item => item.TargetFolio).ThenInclude(item => item!.Guest)
            .Include(item => item.TargetGroupFolio).ThenInclude(item => item!.GroupBooking)
            .Include(item => item.TargetPseudoRoom)
            .AsNoTracking()
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.SourceChargeCategory)
            .ToListAsync();

        var groups = await context.GroupBookings
            .AsNoTracking()
            .OrderByDescending(item => item.ArrivalDate)
            .Select(item => new { item.Id, Name = item.GroupCode + " - " + item.GroupName })
            .ToListAsync();
        GroupBookingOptions = new SelectList(groups, "Id", "Name");

        var reservations = await context.Reservations
            .AsNoTracking()
            .Include(item => item.Guest)
            .OrderByDescending(item => item.ArrivalDate)
            .Select(item => new
            {
                item.Id,
                Name = item.ConfirmationNumber + " - " + (item.Guest == null ? "Guest" : item.Guest.FirstName + " " + item.Guest.LastName)
            })
            .ToListAsync();
        ReservationOptions = new SelectList(reservations, "Id", "Name");

        var folios = await context.Folios
            .AsNoTracking()
            .Include(item => item.Guest)
            .OrderByDescending(item => item.Id)
            .Select(item => new
            {
                item.Id,
                Name = item.FolioNumber + " - " + (item.Guest == null ? "Guest" : item.Guest.FirstName + " " + item.Guest.LastName)
            })
            .ToListAsync();
        FolioOptions = new SelectList(folios, "Id", "Name");

        var groupFolios = await context.GroupFolios
            .AsNoTracking()
            .Include(item => item.GroupBooking)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new { item.Id, Name = item.FolioName + " - " + item.GroupBooking!.GroupName })
            .ToListAsync();
        GroupFolioOptions = new SelectList(groupFolios, "Id", "Name");

        var pseudoRooms = await context.PseudoRooms
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.PseudoRoomCode)
            .Select(item => new { item.Id, Name = item.PseudoRoomCode + " - " + item.PseudoRoomName })
            .ToListAsync();
        PseudoRoomOptions = new SelectList(pseudoRooms, "Id", "Name");
    }
}
