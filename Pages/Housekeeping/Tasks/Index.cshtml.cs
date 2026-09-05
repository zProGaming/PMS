using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Services;
using Vantage.PMS.Models.Housekeeping;

namespace Vantage.PMS.Pages.Housekeeping.Tasks;

public class IndexModel(ApplicationDbContext context, HousekeepingWorkflowService workflow) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<HousekeepingTask> Tasks { get; set; } = new List<HousekeepingTask>();

    public HousekeepingTask? SelectedTask { get; private set; }

    public int? FilterTaskId { get; private set; }

    public async Task OnGetAsync(int? taskId)
    {
        FilterTaskId = taskId;
        Tasks = await _context.HousekeepingTasks
            .Include(task => task.Room)
            .AsNoTracking()
            .Where(task => !taskId.HasValue || task.Id == taskId.Value)
            .OrderBy(task => task.TaskStatus == HousekeepingTaskStatus.Completed)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Room!.RoomNumber)
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetCompleteNativeAsync(int id)
    {
        SelectedTask = await _context.HousekeepingTasks
            .Include(task => task.Room)
            .AsNoTracking()
            .FirstOrDefaultAsync(task => task.Id == id);

        if (SelectedTask is null)
        {
            return NotFound();
        }

        return new PartialViewResult
        {
            ViewName = "_CompleteNative",
            ViewData = new ViewDataDictionary<IndexModel>(ViewData, this)
        };
    }

    public async Task<IActionResult> OnPostCompleteAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var errors = await workflow.CompleteTaskAsync(id.Value, User);
        TempData[errors.Count > 0 ? "ErrorMessage" : "SuccessMessage"] = errors.Count > 0
            ? string.Join(" ", errors)
            : "Task completed. A vacant dirty room becomes Clean once all tasks are done. Supervisor inspection and release are still required.";

        return RedirectToPage("./Index");
    }
}
