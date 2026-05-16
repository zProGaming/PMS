using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.ManagementAI;

namespace Vantage.PMS.Pages.ManagementAI.ActionLog;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<AIActionLog> ActionLogs { get; set; } = new List<AIActionLog>();

    public async Task OnGetAsync()
    {
        ActionLogs = await _context.AIActionLogs
            .AsNoTracking()
            .Include(log => log.RelatedInsight)
            .OrderByDescending(log => log.ActionDate)
            .Take(250)
            .ToListAsync();
    }
}
