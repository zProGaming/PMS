using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Models.Housekeeping;

namespace Vantage.PMS.Pages.GuestPortalManagement.ServiceRequests;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private static readonly GuestServiceRequestType[] HousekeepingRequestTypes =
    [
        GuestServiceRequestType.Housekeeping,
        GuestServiceRequestType.Amenities,
        GuestServiceRequestType.ExtraTowels,
        GuestServiceRequestType.ExtraPillows
    ];

    public IList<GuestServiceRequest> Requests { get; set; } = new List<GuestServiceRequest>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Requests = await _context.GuestServiceRequests
            .Include(request => request.Guest)
            .Include(request => request.Room)
            .AsNoTracking()
            .OrderBy(request => request.Status == GuestServiceRequestStatus.Completed)
            .ThenByDescending(request => request.Priority)
            .ThenByDescending(request => request.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int id, GuestServiceRequestStatus status)
    {
        var request = await _context.GuestServiceRequests.FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        request.Status = status;
        if (status == GuestServiceRequestStatus.Assigned && string.IsNullOrWhiteSpace(request.AssignedTo))
        {
            request.AssignedTo = User.Identity?.Name ?? "Staff";
        }

        if (status == GuestServiceRequestStatus.Completed)
        {
            request.CompletedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        StatusMessage = $"Service request marked {status}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateHousekeepingTaskAsync(int id)
    {
        var request = await _context.GuestServiceRequests.FirstOrDefaultAsync(request => request.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        if (!CanCreateHousekeepingTask(request) || request.RoomId is null)
        {
            StatusMessage = "A housekeeping task can only be created for housekeeping or amenity requests with an assigned room.";
            return RedirectToPage();
        }

        _context.HousekeepingTasks.Add(new HousekeepingTask
        {
            RoomId = request.RoomId.Value,
            AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? "Housekeeping" : request.AssignedTo,
            Priority = MapPriority(request.Priority),
            TaskStatus = HousekeepingTaskStatus.Open,
            Notes = $"Guest request #{request.Id}: {request.Description}"
        });

        request.Status = GuestServiceRequestStatus.Assigned;
        request.AssignedTo ??= "Housekeeping";
        await _context.SaveChangesAsync();
        StatusMessage = "Housekeeping task created from guest service request.";
        return RedirectToPage();
    }

    public bool CanCreateHousekeepingTask(GuestServiceRequest request)
    {
        return HousekeepingRequestTypes.Contains(request.RequestType) && request.RoomId is not null;
    }

    private static HousekeepingTaskPriority MapPriority(GuestServiceRequestPriority priority)
    {
        return priority switch
        {
            GuestServiceRequestPriority.Low => HousekeepingTaskPriority.Low,
            GuestServiceRequestPriority.High => HousekeepingTaskPriority.High,
            GuestServiceRequestPriority.Urgent => HousekeepingTaskPriority.Urgent,
            _ => HousekeepingTaskPriority.Normal
        };
    }
}
