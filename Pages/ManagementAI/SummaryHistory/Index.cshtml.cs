using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.ManagementAI;

namespace Vantage.PMS.Pages.ManagementAI.SummaryHistory;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<ManagementDailySummary> Summaries { get; set; } = new List<ManagementDailySummary>();

    public async Task OnGetAsync()
    {
        Summaries = await _context.ManagementDailySummaries
            .AsNoTracking()
            .OrderByDescending(summary => summary.BusinessDate)
            .ThenByDescending(summary => summary.CreatedAt)
            .Take(90)
            .ToListAsync();
    }
}
