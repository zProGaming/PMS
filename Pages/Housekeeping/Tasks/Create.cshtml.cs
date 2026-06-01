using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Housekeeping;

namespace Vantage.PMS.Pages.Housekeeping.Tasks;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public HousekeepingTask HousekeepingTask { get; set; } = new()
    {
        TaskStatus = HousekeepingTaskStatus.Open,
        Priority = HousekeepingTaskPriority.Normal
    };

    public SelectList RoomOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> PriorityOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? roomId)
    {
        await LoadCreateFormAsync(roomId);
        return Page();
    }

    public async Task<IActionResult> OnGetNativeAsync(int? roomId)
    {
        await LoadCreateFormAsync(roomId);
        return NativePartial();
    }

    private async Task LoadCreateFormAsync(int? roomId)
    {
        await LoadSelectListsAsync(roomId);

        if (roomId is not null)
        {
            HousekeepingTask.RoomId = roomId.Value;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateTask();

        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(HousekeepingTask.RoomId);
            return NativePartialOrPage();
        }

        HousekeepingTask.TaskStatus = HousekeepingTaskStatus.Open;
        HousekeepingTask.StartedAt = null;
        HousekeepingTask.CompletedAt = null;

        _context.HousekeepingTasks.Add(HousekeepingTask);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task LoadSelectListsAsync(object? selectedRoom = null)
    {
        var rooms = await _context.Rooms
            .Include(room => room.Property)
            .Include(room => room.RoomType)
            .AsNoTracking()
            .Where(room => room.IsActive)
            .OrderBy(room => room.Property!.Name)
            .ThenBy(room => room.RoomNumber)
            .ToListAsync();

        RoomOptions = new SelectList(
            rooms.Select(room => new
            {
                room.Id,
                Name = $"{room.Property?.Name} - {room.RoomNumber} ({room.RoomType?.Name})"
            }),
            "Id",
            "Name",
            selectedRoom);

        PriorityOptions = Enum.GetValues<HousekeepingTaskPriority>()
            .Select(priority => new SelectListItem
            {
                Value = priority.ToString(),
                Text = priority.ToString(),
                Selected = priority == HousekeepingTask.Priority
            });
    }

    private void ValidateTask()
    {
        if (HousekeepingTask.RoomId <= 0)
        {
            ModelState.AddModelError("HousekeepingTask.RoomId", "Room is required.");
        }

        if (string.IsNullOrWhiteSpace(HousekeepingTask.AssignedTo))
        {
            ModelState.AddModelError("HousekeepingTask.AssignedTo", "Assigned to is required.");
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
            ViewName = "_CreateNative",
            ViewData = new ViewDataDictionary<CreateModel>(ViewData, this)
        };
    }
}
