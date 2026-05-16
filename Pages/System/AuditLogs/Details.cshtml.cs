using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Pages.System.AuditLogs;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public AuditLog? AuditLog { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        AuditLog = await _context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(log => log.Id == id);
        return AuditLog is null ? NotFound() : Page();
    }
}
