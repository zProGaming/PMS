using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.ManagementAI;

namespace Vantage.PMS.Pages.ManagementAI.SummaryHistory;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public ManagementDailySummary? Summary { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Summary = await _context.ManagementDailySummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(summary => summary.Id == id);

        return Summary is null ? NotFound() : Page();
    }
}
