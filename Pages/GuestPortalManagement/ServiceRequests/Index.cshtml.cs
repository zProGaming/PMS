using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Services;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Models.Housekeeping;

namespace Vantage.PMS.Pages.GuestPortalManagement.ServiceRequests;

public class IndexModel(ApplicationDbContext context, HousekeepingWorkflowService workflow) : PageModel
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
        var errors = await workflow.CreateGuestTaskAsync(id, User);
        StatusMessage = errors.Count > 0 ? string.Join(" ", errors) : "Housekeeping task linked to the guest request. Repeated submissions do not create duplicates.";
        return RedirectToPage();
    }

    public bool CanCreateHousekeepingTask(GuestServiceRequest request)
    {
        return HousekeepingRequestTypes.Contains(request.RequestType) && request.RoomId is not null;
    }

}
