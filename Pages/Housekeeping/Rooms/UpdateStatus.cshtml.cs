using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Services;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Housekeeping.Rooms;

public class UpdateStatusModel(ApplicationDbContext context, HousekeepingWorkflowService workflow) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public Room Room { get; set; } = default!;

    [BindProperty]
    public RoomStatus TargetStatus { get; set; }

    [BindProperty]
    public RoomStatus? ExpectedStatus { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var loadResult = await LoadStatusFormAsync(id);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return Page();
    }

    public async Task<IActionResult> OnGetNativeAsync(int? id)
    {
        var loadResult = await LoadStatusFormAsync(id);
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

        var room = await LoadRoomAsync(id.Value, asTracking: true);
        if (room is null)
        {
            return NotFound();
        }

        Room = room;
        if (ModelState.IsValid)
            foreach (var error in await workflow.UpdateRoomAsync(id.Value, ExpectedStatus, TargetStatus, Notes, User))
                ModelState.AddModelError(string.Empty, error);
        if (!ModelState.IsValid)
        {
            Room = (await LoadRoomAsync(id.Value, asTracking: false))!;
            LoadStatusOptions(Room.Status);
            return NativePartialOrPage();
        }
        TempData["SuccessMessage"] = "Room status updated. Cleaning, inspection, and release are separate recorded steps.";

        return RedirectToPage("/Housekeeping/Index");
    }

    private async Task<IActionResult?> LoadStatusFormAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var room = await LoadRoomAsync(id.Value, asTracking: false);
        if (room is null)
        {
            return NotFound();
        }

        Room = room;
        Notes = room.StatusNotes;
        ExpectedStatus = room.Status;
        LoadStatusOptions(room.Status);

        return null;
    }

    private async Task<Room?> LoadRoomAsync(int id, bool asTracking)
    {
        var query = _context.Rooms
            .Include(room => room.Property)
            .Include(room => room.RoomType)
            .Where(room => room.Id == id);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private void LoadStatusOptions(RoomStatus currentStatus)
    {
        StatusOptions = HousekeepingWorkflowService.AllowedStatuses(currentStatus, User)
            .Select(status => new SelectListItem
            {
                Value = status.ToString(),
                Text = Vantage.PMS.Presentation.UiText.Label(status.ToString()),
                Selected = status == TargetStatus
            });
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
            ViewName = "_UpdateStatusNative",
            ViewData = new ViewDataDictionary<UpdateStatusModel>(ViewData, this)
        };
    }
}
