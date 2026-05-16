using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.GuestPortal;

namespace Vantage.PMS.Pages.GuestPortalManagement.Feedback;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<GuestFeedback> Feedbacks { get; set; } = new List<GuestFeedback>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Feedbacks = await _context.GuestFeedbacks
            .Include(feedback => feedback.Guest)
            .Include(feedback => feedback.Reservation)
            .AsNoTracking()
            .OrderByDescending(feedback => feedback.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id, string? resolutionNotes)
    {
        var feedback = await _context.GuestFeedbacks.FindAsync(id);
        if (feedback is null)
        {
            return NotFound();
        }

        feedback.IsResolved = true;
        feedback.ResolutionNotes = resolutionNotes;
        await _context.SaveChangesAsync();
        StatusMessage = "Feedback marked resolved.";
        return RedirectToPage();
    }
}
