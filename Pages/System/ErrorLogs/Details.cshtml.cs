using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.System.ErrorLogs;

public class DetailsModel(ApplicationDbContext context, SystemErrorLogService errorLogService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly SystemErrorLogService _errorLogService = errorLogService;

    [BindProperty]
    public string? Notes { get; set; }

    public SystemErrorLog? ErrorLog { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ErrorLog = await _context.SystemErrorLogs.AsNoTracking().FirstOrDefaultAsync(log => log.Id == id);
        return ErrorLog is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        await _errorLogService.MarkResolvedAsync(id, User.Identity?.Name ?? "System", Notes);
        return RedirectToPage("./Details", new { id });
    }
}
