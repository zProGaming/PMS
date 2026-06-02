using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Groups.PseudoRooms;

public class IndexModel(ApplicationDbContext context, AuditLogService auditLogService) : PageModel
{
    public IList<PseudoRoom> PseudoRooms { get; private set; } = [];
    public SelectList SalesAccountOptions { get; private set; } = null!;
    public SelectList BanquetEventOptions { get; private set; } = null!;
    public SelectList GroupBookingOptions { get; private set; } = null!;

    [BindProperty]
    public PseudoRoom Input { get; set; } = new() { IsActive = true };

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnGetNativeAsync()
    {
        await LoadAsync();
        return NativePartial();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        Input.PseudoRoomCode = (Input.PseudoRoomCode ?? string.Empty).Trim().ToUpperInvariant();
        Input.PseudoRoomName = (Input.PseudoRoomName ?? string.Empty).Trim();
        Input.CreatedAt = DateTime.Now;
        Input.CreatedBy = User.Identity?.Name ?? "System";
        Input.IsActive = true;

        if (string.IsNullOrWhiteSpace(Input.PseudoRoomCode))
        {
            ModelState.AddModelError("Input.PseudoRoomCode", "Pseudo room code is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.PseudoRoomName))
        {
            ModelState.AddModelError("Input.PseudoRoomName", "Pseudo room name is required.");
        }

        if (await context.PseudoRooms.AnyAsync(item => item.PseudoRoomCode == Input.PseudoRoomCode))
        {
            ModelState.AddModelError("Input.PseudoRoomCode", "Pseudo room code must be unique.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return NativePartialOrPage();
        }

        context.PseudoRooms.Add(Input);
        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Create, "Group Management", nameof(PseudoRoom), Input.Id.ToString(), null, new { Input.PseudoRoomCode, Input.PseudoRoomName, Input.PseudoRoomType });
        return RedirectToPage();
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
            ViewName = "_CreateNative",
            ViewData = new ViewDataDictionary<IndexModel>(ViewData, this)
        };
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var pseudoRoom = await context.PseudoRooms.FindAsync(id);
        if (pseudoRoom is not null)
        {
            pseudoRoom.IsActive = !pseudoRoom.IsActive;
            await context.SaveChangesAsync();
            await auditLogService.LogAsync(AuditActionType.Update, "Group Management", nameof(PseudoRoom), pseudoRoom.Id.ToString(), null, new { pseudoRoom.PseudoRoomCode, pseudoRoom.IsActive });
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        PseudoRooms = await context.PseudoRooms
            .Include(item => item.LinkedSalesAccount)
            .Include(item => item.LinkedBanquetEvent)
            .Include(item => item.LinkedGroupBooking)
            .AsNoTracking()
            .OrderBy(item => item.PseudoRoomCode)
            .ToListAsync();

        SalesAccountOptions = new SelectList(await context.SalesAccounts.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.AccountName).Select(item => new { item.Id, item.AccountName }).ToListAsync(), "Id", "AccountName");
        BanquetEventOptions = new SelectList(await context.BanquetEvents.AsNoTracking().OrderByDescending(item => item.EventDate).Select(item => new { item.Id, item.EventName }).ToListAsync(), "Id", "EventName");
        GroupBookingOptions = new SelectList(await context.GroupBookings.AsNoTracking().OrderByDescending(item => item.ArrivalDate).Select(item => new { item.Id, Name = item.GroupCode + " - " + item.GroupName }).ToListAsync(), "Id", "Name");
    }
}
