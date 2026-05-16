using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.System.ErrorLogs;

public class IndexModel(ApplicationDbContext context, SystemErrorLogService errorLogService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly SystemErrorLogService _errorLogService = errorLogService;
    private const int PageSize = 50;

    [BindProperty(SupportsGet = true)]
    public bool? Resolved { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty]
    public string? ResolutionNotes { get; set; }

    public int TotalPages { get; set; }

    public IList<SystemErrorLog> ErrorLogs { get; set; } = new List<SystemErrorLog>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        await _errorLogService.MarkResolvedAsync(id, User.Identity?.Name ?? "System", ResolutionNotes);
        StatusMessage = "System error marked as resolved.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        CurrentPage = Math.Max(1, CurrentPage);
        var query = _context.SystemErrorLogs.AsNoTracking();
        if (Resolved is not null)
        {
            query = query.Where(log => log.IsResolved == Resolved.Value);
        }

        var totalRecords = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)PageSize));
        CurrentPage = Math.Min(CurrentPage, TotalPages);
        ErrorLogs = await query
            .OrderByDescending(log => log.ErrorDate)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
