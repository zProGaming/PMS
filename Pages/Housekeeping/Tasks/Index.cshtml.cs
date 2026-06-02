using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Housekeeping;

namespace Vantage.PMS.Pages.Housekeeping.Tasks;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<HousekeepingTask> Tasks { get; set; } = new List<HousekeepingTask>();

    public HousekeepingTask? SelectedTask { get; private set; }

    public async Task OnGetAsync()
    {
        Tasks = await _context.HousekeepingTasks
            .Include(task => task.Room)
            .AsNoTracking()
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

        var task = await _context.HousekeepingTasks.FindAsync(id);
        if (task is null)
        {
            return NotFound();
        }

        task.TaskStatus = HousekeepingTaskStatus.Completed;
        task.CompletedAt = DateTime.Now;

        if (task.StartedAt is null)
        {
            task.StartedAt = task.CompletedAt;
        }

        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
