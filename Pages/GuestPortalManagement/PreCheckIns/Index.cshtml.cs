using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.GuestPortal;

namespace Vantage.PMS.Pages.GuestPortalManagement.PreCheckIns;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<GuestPreCheckIn> Submissions { get; set; } = new List<GuestPreCheckIn>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Submissions = await _context.GuestPreCheckIns
            .Include(item => item.Guest)
            .Include(item => item.Reservation)
            .AsNoTracking()
            .OrderByDescending(item => item.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int id, GuestPreCheckInStatus status)
    {
        var submission = await _context.GuestPreCheckIns.FindAsync(id);
        if (submission is null)
        {
            return NotFound();
        }

        submission.Status = status;
        await _context.SaveChangesAsync();
        StatusMessage = $"Pre-check-in marked {status}.";
        return RedirectToPage();
    }
}
