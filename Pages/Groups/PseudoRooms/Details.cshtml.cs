using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Groups.PseudoRooms;

public class DetailsModel(ApplicationDbContext context, AuditLogService auditLogService) : PageModel
{
    public PseudoRoom PseudoRoom { get; private set; } = default!;
    public SelectList SalesAccountOptions { get; private set; } = null!;
    public SelectList BanquetEventOptions { get; private set; } = null!;
    public SelectList GroupBookingOptions { get; private set; } = null!;

    [BindProperty]
    public PseudoRoom EditInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadOrNotFoundAsync(id);
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id)
    {
        var pseudoRoom = await context.PseudoRooms.FirstOrDefaultAsync(item => item.Id == id);
        if (pseudoRoom is null)
        {
            return NotFound();
        }

        EditInput.PseudoRoomCode = (EditInput.PseudoRoomCode ?? string.Empty).Trim().ToUpperInvariant();
        EditInput.PseudoRoomName = (EditInput.PseudoRoomName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(EditInput.PseudoRoomCode))
        {
            ModelState.AddModelError("EditInput.PseudoRoomCode", "Pseudo room code is required.");
        }

        if (string.IsNullOrWhiteSpace(EditInput.PseudoRoomName))
        {
            ModelState.AddModelError("EditInput.PseudoRoomName", "Pseudo room name is required.");
        }

        if (await context.PseudoRooms.AnyAsync(item => item.Id != id && item.PseudoRoomCode == EditInput.PseudoRoomCode))
        {
            ModelState.AddModelError("EditInput.PseudoRoomCode", "Pseudo room code must be unique.");
        }

        if (!ModelState.IsValid)
        {
            return await LoadOrNotFoundAsync(id);
        }

        pseudoRoom.PseudoRoomCode = EditInput.PseudoRoomCode;
        pseudoRoom.PseudoRoomName = EditInput.PseudoRoomName;
        pseudoRoom.PseudoRoomType = EditInput.PseudoRoomType;
        pseudoRoom.LinkedSalesAccountId = EditInput.LinkedSalesAccountId;
        pseudoRoom.LinkedBanquetEventId = EditInput.LinkedBanquetEventId;
        pseudoRoom.LinkedGroupBookingId = EditInput.LinkedGroupBookingId;
        pseudoRoom.IsActive = EditInput.IsActive;
        pseudoRoom.Notes = EditInput.Notes;

        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Update, "Group Management", nameof(PseudoRoom), pseudoRoom.Id.ToString(), null, new { pseudoRoom.PseudoRoomCode, pseudoRoom.PseudoRoomName, pseudoRoom.PseudoRoomType, pseudoRoom.IsActive });
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult> LoadOrNotFoundAsync(int id)
    {
        var pseudoRoom = await context.PseudoRooms
            .Include(item => item.LinkedSalesAccount)
            .Include(item => item.LinkedBanquetEvent)
            .Include(item => item.LinkedGroupBooking)
            .Include(item => item.GroupFolios).ThenInclude(item => item.GroupBooking)
            .Include(item => item.GroupFolios).ThenInclude(item => item.Folio).ThenInclude(item => item!.Items)
            .Include(item => item.GroupFolios).ThenInclude(item => item.Folio).ThenInclude(item => item!.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (pseudoRoom is null)
        {
            return NotFound();
        }

        PseudoRoom = pseudoRoom;
        EditInput = new PseudoRoom
        {
            PseudoRoomCode = pseudoRoom.PseudoRoomCode,
            PseudoRoomName = pseudoRoom.PseudoRoomName,
            PseudoRoomType = pseudoRoom.PseudoRoomType,
            LinkedSalesAccountId = pseudoRoom.LinkedSalesAccountId,
            LinkedBanquetEventId = pseudoRoom.LinkedBanquetEventId,
            LinkedGroupBookingId = pseudoRoom.LinkedGroupBookingId,
            IsActive = pseudoRoom.IsActive,
            Notes = pseudoRoom.Notes
        };
        await LoadOptionsAsync();
        return Page();
    }

    private async Task LoadOptionsAsync()
    {
        SalesAccountOptions = new SelectList(await context.SalesAccounts.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.AccountName).Select(item => new { item.Id, item.AccountName }).ToListAsync(), "Id", "AccountName", EditInput.LinkedSalesAccountId);
        BanquetEventOptions = new SelectList(await context.BanquetEvents.AsNoTracking().OrderByDescending(item => item.EventDate).Select(item => new { item.Id, item.EventName }).ToListAsync(), "Id", "EventName", EditInput.LinkedBanquetEventId);
        GroupBookingOptions = new SelectList(await context.GroupBookings.AsNoTracking().OrderByDescending(item => item.ArrivalDate).Select(item => new { item.Id, Name = item.GroupCode + " - " + item.GroupName }).ToListAsync(), "Id", "Name", EditInput.LinkedGroupBookingId);
    }
}
